using FolioTrace.Types;

namespace Services;

public sealed class AggregateMaintenanceCoordinator(
    AggregateMaintenanceOptions options,
    CountryService countryService,
    CurrencyService currencyService,
    FXService fxService,
    FXRateService fxRateService,
    InstrumentService instrumentService,
    InstrumentValueService instrumentValueService,
    AggregateUpdateNotificationService notificationService)
{
    private const int MaxRecentErrors = 10;

    private readonly SemaphoreSlim operationGate = new(1, 1);
    private readonly Lock stateLock = new();
    private readonly Queue<string> recentErrors = new();
    private int eventTriggerScheduled;
    private int pendingEventCount;
    private bool isSuspended;
    private string? suspensionReason;
    private DateTime? suspendedAtUtc;
    private string statusBeforeSuspension = "Idle";
    private AggregateMaintenanceDiagnostics diagnostics = AggregateMaintenanceDiagnostics.Initial(options);

    public AggregateMaintenanceDiagnostics GetDiagnostics()
    {
        lock (stateLock)
        {
            return diagnostics with
            {
                Status = isSuspended ? "Suspended" : diagnostics.Status,
                PendingEventCount = Volatile.Read(ref pendingEventCount),
                IsSuspended = isSuspended,
                SuspensionReason = suspensionReason,
                SuspendedAtUtc = suspendedAtUtc,
                RecentErrors = recentErrors.ToArray()
            };
        }
    }

    public async Task<IAsyncDisposable> SuspendAsync(string reason, CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        SetSuspended(reason);
        return new AggregateMaintenanceSuspension(this);
    }

    public void NotifyEventsCreated(int eventCount)
    {
        if (!options.Enabled || options.EventTriggerCount <= 0 || eventCount <= 0)
            return;

        var pending = Interlocked.Add(ref pendingEventCount, eventCount);
        UpdatePendingEventCount(pending);

        if (pending < options.EventTriggerCount)
            return;

        if (Interlocked.Exchange(ref eventTriggerScheduled, 1) == 1)
            return;

        _ = RunAfterEventDelayAsync();
    }

    public async Task RunAsync(string trigger, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            SetDisabled(trigger);
            return;
        }

        if (IsSuspended())
        {
            RecordSuspendedRun(trigger);
            return;
        }

        if (!await operationGate.WaitAsync(0, cancellationToken))
        {
            RecordSkipped(trigger);
            return;
        }

        var runID = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;

        try
        {
            if (IsSuspended())
            {
                RecordSuspendedRun(trigger);
                return;
            }

            SetRunning(runID, trigger, startedAtUtc);

            var result = new AggregateMaintenanceRunResult();
            var valuationDates = AggregateMaintenanceDateCalculator.CreateValuationDates(options.DateWindows, DateTime.Now);

            foreach (var valuationDate in valuationDates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await WarmAggregate("Countries", valuationDate, countryService.IsCached, countryService.Get, result);
                await WarmAggregate("Currencies", valuationDate, currencyService.IsCached, currencyService.Get, result);
                await WarmAggregate("FXs", valuationDate, fxService.IsCached, fxService.Get, result);
                await WarmAggregate("FXRates", valuationDate, fxRateService.IsCached, fxRateService.Get, result);
                await WarmAggregate("Instruments", valuationDate, instrumentService.IsCached, instrumentService.Get, result);
                await WarmAggregate("InstrumentValues", valuationDate, instrumentValueService.IsCached, instrumentValueService.Get, result);
            }

            SetCompleted(runID, trigger, startedAtUtc, result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SetFailed(runID, trigger, startedAtUtc, "Aggregate maintenance cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            SetFailed(runID, trigger, startedAtUtc, exception.Message);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task RunAfterEventDelayAsync()
    {
        try
        {
            if (options.EventTriggerDelay > TimeSpan.Zero)
                await Task.Delay(options.EventTriggerDelay);

            Interlocked.Exchange(ref pendingEventCount, 0);
            await RunAsync("EventCount");
        }
        finally
        {
            Interlocked.Exchange(ref eventTriggerScheduled, 0);
            UpdatePendingEventCount(Volatile.Read(ref pendingEventCount));
        }
    }

    private static async Task WarmAggregate<TAggregate>(
        string aggregateKind,
        EventDateTime valuationDate,
        Func<EventDateTime, bool> isCached,
        Func<EventDateTime, Task<TAggregate>> get,
        AggregateMaintenanceRunResult result)
    {
        result.ScannedAggregates++;

        if (isCached(valuationDate))
            return;

        result.MissingAggregates++;

        try
        {
            await get(valuationDate);
            result.FixedAggregates++;
        }
        catch (Exception exception)
        {
            result.FailedAggregates++;
            result.Errors.Add($"{aggregateKind} {valuationDate.Value:O}: {exception.Message}");
        }
    }

    private void SetRunning(Guid runID, string trigger, DateTime startedAtUtc)
    {
        AggregateMaintenanceNotification notification;

        lock (stateLock)
        {
            diagnostics = diagnostics with
            {
                Status = "Running",
                ActiveRunID = runID,
                LastTrigger = trigger,
                LastStartedAtUtc = startedAtUtc,
                LastCompletedAtUtc = null,
                LastError = null,
                PendingEventCount = Volatile.Read(ref pendingEventCount)
            };
            notification = CreateNotification(changed: true);
        }

        notificationService.PublishAggregateMaintenance(notification);
    }

    private void SetCompleted(Guid runID, string trigger, DateTime startedAtUtc, AggregateMaintenanceRunResult result)
    {
        AggregateMaintenanceNotification notification;

        lock (stateLock)
        {
            foreach (var error in result.Errors)
                AddRecentError(error);

            diagnostics = diagnostics with
            {
                Status = result.FailedAggregates > 0 ? "CompletedWithErrors" : "Succeeded",
                ActiveRunID = null,
                LastRunID = runID,
                LastTrigger = trigger,
                LastStartedAtUtc = startedAtUtc,
                LastCompletedAtUtc = DateTime.UtcNow,
                LastScannedAggregates = result.ScannedAggregates,
                LastMissingAggregates = result.MissingAggregates,
                LastFixedAggregates = result.FixedAggregates,
                LastFailedAggregates = result.FailedAggregates,
                TotalScannedAggregates = diagnostics.TotalScannedAggregates + result.ScannedAggregates,
                TotalMissingAggregates = diagnostics.TotalMissingAggregates + result.MissingAggregates,
                TotalFixedAggregates = diagnostics.TotalFixedAggregates + result.FixedAggregates,
                TotalFailedAggregates = diagnostics.TotalFailedAggregates + result.FailedAggregates,
                LastError = result.Errors.LastOrDefault(),
                PendingEventCount = Volatile.Read(ref pendingEventCount),
                RecentErrors = recentErrors.ToArray()
            };
            notification = CreateNotification(result.MissingAggregates > 0 || result.FixedAggregates > 0 || result.FailedAggregates > 0);
        }

        notificationService.PublishAggregateMaintenance(notification);
    }

    private void SetFailed(Guid runID, string trigger, DateTime startedAtUtc, string error)
    {
        AggregateMaintenanceNotification notification;

        lock (stateLock)
        {
            AddRecentError(error);
            diagnostics = diagnostics with
            {
                Status = "Failed",
                ActiveRunID = null,
                LastRunID = runID,
                LastTrigger = trigger,
                LastStartedAtUtc = startedAtUtc,
                LastCompletedAtUtc = DateTime.UtcNow,
                LastFailedAggregates = diagnostics.LastFailedAggregates + 1,
                TotalFailedAggregates = diagnostics.TotalFailedAggregates + 1,
                LastError = error,
                PendingEventCount = Volatile.Read(ref pendingEventCount),
                RecentErrors = recentErrors.ToArray()
            };
            notification = CreateNotification(changed: true);
        }

        notificationService.PublishAggregateMaintenance(notification);
    }

    private void RecordSkipped(string trigger)
    {
        AggregateMaintenanceNotification notification;

        lock (stateLock)
        {
            diagnostics = diagnostics with
            {
                LastTrigger = trigger,
                SkippedRunCount = diagnostics.SkippedRunCount + 1,
                PendingEventCount = Volatile.Read(ref pendingEventCount)
            };
            notification = CreateNotification(changed: true);
        }

        notificationService.PublishAggregateMaintenance(notification);
    }

    private void RecordSuspendedRun(string trigger)
    {
        AggregateMaintenanceNotification notification;

        lock (stateLock)
        {
            diagnostics = diagnostics with
            {
                Status = "Suspended",
                LastTrigger = trigger,
                SkippedRunCount = diagnostics.SkippedRunCount + 1,
                SuspendedRunCount = diagnostics.SuspendedRunCount + 1,
                PendingEventCount = Volatile.Read(ref pendingEventCount),
                IsSuspended = isSuspended,
                SuspensionReason = suspensionReason,
                SuspendedAtUtc = suspendedAtUtc
            };
            notification = CreateNotification(changed: true);
        }

        notificationService.PublishAggregateMaintenance(notification);
    }

    private void SetDisabled(string trigger)
    {
        AggregateMaintenanceNotification notification;

        lock (stateLock)
        {
            diagnostics = diagnostics with
            {
                Status = "Disabled",
                LastTrigger = trigger,
                PendingEventCount = Volatile.Read(ref pendingEventCount)
            };
            notification = CreateNotification(changed: true);
        }

        notificationService.PublishAggregateMaintenance(notification);
    }

    private bool IsSuspended()
    {
        lock (stateLock)
        {
            return isSuspended;
        }
    }

    private void SetSuspended(string reason)
    {
        AggregateMaintenanceNotification notification;

        lock (stateLock)
        {
            isSuspended = true;
            suspensionReason = string.IsNullOrWhiteSpace(reason) ? "Maintenance suspended." : reason;
            suspendedAtUtc = DateTime.UtcNow;
            statusBeforeSuspension = diagnostics.Status == "Suspended" ? statusBeforeSuspension : diagnostics.Status;
            diagnostics = diagnostics with
            {
                Status = "Suspended",
                IsSuspended = true,
                SuspensionReason = suspensionReason,
                SuspendedAtUtc = suspendedAtUtc,
                PendingEventCount = Volatile.Read(ref pendingEventCount)
            };
            notification = CreateNotification(changed: true);
        }

        notificationService.PublishAggregateMaintenance(notification);
    }

    private void Resume()
    {
        AggregateMaintenanceNotification notification;

        lock (stateLock)
        {
            isSuspended = false;
            suspensionReason = null;
            suspendedAtUtc = null;
            diagnostics = diagnostics with
            {
                Status = statusBeforeSuspension,
                IsSuspended = false,
                SuspensionReason = null,
                SuspendedAtUtc = null,
                PendingEventCount = Volatile.Read(ref pendingEventCount)
            };
            notification = CreateNotification(changed: true);
        }

        notificationService.PublishAggregateMaintenance(notification);
        operationGate.Release();
    }

    private void UpdatePendingEventCount(int pending)
    {
        AggregateMaintenanceNotification notification;

        lock (stateLock)
        {
            diagnostics = diagnostics with { PendingEventCount = pending };
            notification = CreateNotification(changed: true);
        }

        notificationService.PublishAggregateMaintenance(notification);
    }

    private void AddRecentError(string error)
    {
        recentErrors.Enqueue(error);
        while (recentErrors.Count > MaxRecentErrors)
            recentErrors.Dequeue();
    }

    private AggregateMaintenanceNotification CreateNotification(bool changed) =>
        new(
            "AggregateMaintenance",
            diagnostics.Status,
            diagnostics.LastTrigger,
            changed,
            DateTime.UtcNow);

    private sealed class AggregateMaintenanceSuspension(AggregateMaintenanceCoordinator coordinator) : IAsyncDisposable
    {
        private int disposed;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
                coordinator.Resume();

            return ValueTask.CompletedTask;
        }
    }
}

public sealed record AggregateMaintenanceDiagnostics(
    bool Enabled,
    TimeSpan PeriodicDelay,
    int EventTriggerCount,
    TimeSpan EventTriggerDelay,
    int DaysFromToday,
    int EndOfWeeksFromToday,
    int EndOfMonthsFromToday,
    string Status,
    Guid? ActiveRunID,
    Guid? LastRunID,
    string? LastTrigger,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc,
    int LastScannedAggregates,
    int LastMissingAggregates,
    int LastFixedAggregates,
    int LastFailedAggregates,
    long TotalScannedAggregates,
    long TotalMissingAggregates,
    long TotalFixedAggregates,
    long TotalFailedAggregates,
    int SkippedRunCount,
    int PendingEventCount,
    bool IsSuspended,
    string? SuspensionReason,
    DateTime? SuspendedAtUtc,
    int SuspendedRunCount,
    string? LastError,
    IReadOnlyList<string> RecentErrors)
{
    public static AggregateMaintenanceDiagnostics Initial(AggregateMaintenanceOptions options) =>
        new(
            options.Enabled,
            options.PeriodicDelay,
            options.EventTriggerCount,
            options.EventTriggerDelay,
            options.DateWindows.DaysFromToday,
            options.DateWindows.EndOfWeeksFromToday,
            options.DateWindows.EndOfMonthsFromToday,
            options.Enabled ? "Idle" : "Disabled",
            null,
            null,
            null,
            null,
            null,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            null,
            null,
            0,
            null,
            []);
}

internal sealed class AggregateMaintenanceRunResult
{
    public int ScannedAggregates { get; set; }

    public int MissingAggregates { get; set; }

    public int FixedAggregates { get; set; }

    public int FailedAggregates { get; set; }

    public List<string> Errors { get; } = [];
}
