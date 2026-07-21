using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace Test;

public sealed class AggregateMaintenanceCoordinatorTests
{
    [Fact]
    public async Task RunAsync_WarmsMissingAggregateCaches()
    {
        var coordinator = await CreateCoordinator();

        await coordinator.RunAsync("Test");

        var diagnostics = coordinator.GetDiagnostics();
        Assert.Equal("Succeeded", diagnostics.Status);
        Assert.Equal(diagnostics.LastScannedAggregates, diagnostics.LastMissingAggregates);
        Assert.Equal(diagnostics.LastMissingAggregates, diagnostics.LastFixedAggregates);
        Assert.True(diagnostics.LastScannedAggregates > 0);
        Assert.Equal(0, diagnostics.LastFailedAggregates);
    }

    [Fact]
    public async Task RunAsync_DoesNotFixAlreadyCachedAggregates()
    {
        var coordinator = await CreateCoordinator();

        await coordinator.RunAsync("First");
        await coordinator.RunAsync("Second");

        var diagnostics = coordinator.GetDiagnostics();
        Assert.Equal("Succeeded", diagnostics.Status);
        Assert.True(diagnostics.LastScannedAggregates > 0);
        Assert.Equal(0, diagnostics.LastMissingAggregates);
        Assert.Equal(0, diagnostics.LastFixedAggregates);
    }

    [Fact]
    public async Task NotifyEventsCreated_SchedulesDelayedEventCountRun()
    {
        var coordinator = await CreateCoordinator(new AggregateMaintenanceOptions
        {
            Enabled = true,
            EventTriggerCount = 2,
            EventTriggerDelay = TimeSpan.Zero,
            DateWindows = new AggregateMaintenanceDateWindowOptions()
        });

        coordinator.NotifyEventsCreated(1);
        Assert.Equal(1, coordinator.GetDiagnostics().PendingEventCount);

        coordinator.NotifyEventsCreated(1);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var diagnostics = coordinator.GetDiagnostics();
            if (diagnostics.LastTrigger == "EventCount")
            {
                Assert.Equal("Succeeded", diagnostics.Status);
                Assert.Equal(0, diagnostics.PendingEventCount);
                return;
            }

            await Task.Delay(25);
        }

        Assert.Fail("Event-count aggregate maintenance run was not scheduled.");
    }

    [Fact]
    public async Task RunAsync_SkipsWhileSuspended()
    {
        var coordinator = await CreateCoordinator();

        await using (await coordinator.SuspendAsync("Test suspension."))
        {
            await coordinator.RunAsync("Periodic");

            var suspendedDiagnostics = coordinator.GetDiagnostics();
            Assert.True(suspendedDiagnostics.IsSuspended);
            Assert.Equal("Suspended", suspendedDiagnostics.Status);
            Assert.Equal("Test suspension.", suspendedDiagnostics.SuspensionReason);
            Assert.NotNull(suspendedDiagnostics.SuspendedAtUtc);
            Assert.Equal(1, suspendedDiagnostics.SuspendedRunCount);
            Assert.Equal(0, suspendedDiagnostics.LastScannedAggregates);
        }

        var resumedDiagnostics = coordinator.GetDiagnostics();
        Assert.False(resumedDiagnostics.IsSuspended);
        Assert.Null(resumedDiagnostics.SuspensionReason);
        Assert.Null(resumedDiagnostics.SuspendedAtUtc);
    }

    [Fact]
    public async Task NotifyEventsCreated_SkipsDelayedRunWhileSuspended()
    {
        var coordinator = await CreateCoordinator(new AggregateMaintenanceOptions
        {
            Enabled = true,
            EventTriggerCount = 1,
            EventTriggerDelay = TimeSpan.Zero,
            DateWindows = new AggregateMaintenanceDateWindowOptions()
        });

        await using (await coordinator.SuspendAsync("Test event suspension."))
        {
            coordinator.NotifyEventsCreated(1);

            for (var attempt = 0; attempt < 20; attempt++)
            {
                var diagnostics = coordinator.GetDiagnostics();
                if (diagnostics.SuspendedRunCount > 0)
                {
                    Assert.Equal("EventCount", diagnostics.LastTrigger);
                    Assert.Equal(0, diagnostics.LastScannedAggregates);
                    return;
                }

                await Task.Delay(25);
            }
        }

        Assert.Fail("Event-count aggregate maintenance run was not skipped while suspended.");
    }

    [Fact]
    public async Task SuspendAsync_WaitsForActiveMaintenanceRun()
    {
        var coordinator = await CreateCoordinator();
        var runTask = coordinator.RunAsync("Test");

        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (coordinator.GetDiagnostics().Status == "Running")
                break;

            await Task.Delay(10);
        }

        await using var suspension = await coordinator.SuspendAsync("After run.");

        Assert.True(runTask.IsCompleted);
        Assert.True(coordinator.GetDiagnostics().IsSuspended);
    }

    [Fact]
    public async Task RunAsync_PersistsSnapshots_ForBoundariesInTheWarmTier()
    {
        var (coordinator, snapshotRepository) = await CreateCoordinatorWithSnapshots(new AggregateMaintenanceOptions
        {
            Enabled = true,
            EventTriggerCount = 100,
            EventTriggerDelay = TimeSpan.Zero,
            SnapshotEligibleAfterDays = 0,
            DateWindows = new AggregateMaintenanceDateWindowOptions { DaysFromToday = 2 }
        });

        await coordinator.RunAsync("Test");

        var aggregateKinds = snapshotRepository.Snapshots.Select(snapshot => snapshot.AggregateKind).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("FXs", aggregateKinds);
        Assert.Contains("FXRates", aggregateKinds);
        Assert.Contains("Instruments", aggregateKinds);
        Assert.Contains("InstrumentValues", aggregateKinds);
        Assert.Contains("Tickets", aggregateKinds);
        Assert.Contains("HoldingPositions", aggregateKinds);
        var diagnostics = coordinator.GetDiagnostics();
        Assert.True(diagnostics.LastFailedAggregates == 0, string.Join(Environment.NewLine, diagnostics.RecentErrors));
    }

    [Fact]
    public async Task RunAsync_DoesNotPersistSnapshots_ForBoundariesInTheHotTier()
    {
        var (coordinator, snapshotRepository) = await CreateCoordinatorWithSnapshots(new AggregateMaintenanceOptions
        {
            Enabled = true,
            EventTriggerCount = 100,
            EventTriggerDelay = TimeSpan.Zero,
            SnapshotEligibleAfterDays = 14,
            DateWindows = new AggregateMaintenanceDateWindowOptions { DaysFromToday = 0 }
        });

        await coordinator.RunAsync("Test");

        Assert.Empty(snapshotRepository.Snapshots);
    }

    private static async Task<AggregateMaintenanceCoordinator> CreateCoordinator(AggregateMaintenanceOptions? options = null) =>
        (await CreateCoordinatorWithSnapshots(options)).Coordinator;

    private static async Task<(AggregateMaintenanceCoordinator Coordinator, FakeAggregateSnapshotRepository SnapshotRepository)> CreateCoordinatorWithSnapshots(AggregateMaintenanceOptions? options = null)
    {
        options ??= new AggregateMaintenanceOptions
        {
            Enabled = true,
            EventTriggerCount = 100,
            EventTriggerDelay = TimeSpan.Zero,
            DateWindows = new AggregateMaintenanceDateWindowOptions()
        };

        var eventRepository = new TestEventRepository();
        var seedRepository = new SeedRepository(eventRepository);
        await seedRepository.Build();

        var snapshotRepository = new FakeAggregateSnapshotRepository();
        var countryService = new CountryService(eventRepository);
        var accountService = new AccountService(eventRepository);
        var brokerService = new BrokerService(eventRepository);
        var currencyService = new CurrencyService(eventRepository);
        var fxService = new FXService(eventRepository, snapshotRepository: snapshotRepository);
        var fxRateService = new FXRateService(eventRepository, snapshotRepository: snapshotRepository, fxService: fxService);
        var holdingService = new HoldingService(eventRepository);
        var instrumentService = new InstrumentService(eventRepository, snapshotRepository: snapshotRepository);
        var instrumentValueService = new InstrumentValueService(eventRepository, snapshotRepository: snapshotRepository, instrumentService: instrumentService);
        var ticketService = new TicketService(eventRepository, snapshotRepository: snapshotRepository);
        var holdingPositionService = new HoldingPositionService(eventRepository, holdingService, accountService, instrumentService, snapshotRepository);

        var coordinator = new AggregateMaintenanceCoordinator(
            options,
            accountService,
            brokerService,
            countryService,
            currencyService,
            fxService,
            fxRateService,
            holdingService,
            holdingPositionService,
            instrumentService,
            instrumentValueService,
            new AggregateUpdateNotificationService(),
            ticketService);

        return (coordinator, snapshotRepository);
    }

    private sealed class TestEventRepository : IEventRepository
    {
        private readonly Dictionary<Guid, List<IAuditEventBase>> streams = [];
        private readonly Dictionary<Guid, IAuditEventBase> eventsById = [];

        public EventRepositoryCacheDiagnostics GetCacheDiagnostics() => new(true, streams.Count, eventsById.Count, 0, 0, []);

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            streams.Clear();
            eventsById.Clear();
            return Task.CompletedTask;
        }

        public Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default)
            where T : class, IAuditEventBase =>
            Task.FromResult(eventsById.TryGetValue(eventId.Value, out var @event) ? @event as T : null);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                streams.TryGetValue(streamId, out var events) && events.Count > 0
                    ? events[^1].EventID
                    : null);
        }

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default)
        {
            if (!streams.TryGetValue(streamId, out var events))
                return Task.FromResult<EventID?>(null);

            IEventBase? latest = null;
            foreach (var @event in events)
            {
                if (@event is not IEventBase timedEvent)
                    continue;

                if (timedEvent.EventDateTime.Value > valuationDateTime || (asOfDateTime.HasValue && timedEvent.AuditDateTime.Value > asOfDateTime.Value))
                    continue;

                if (latest is null || CompareEventOrder(timedEvent, latest) > 0)
                    latest = timedEvent;
            }

            return Task.FromResult(latest?.EventID);
        }

        public Task<IReadOnlyList<IAuditEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IAuditEventBase>>(streams.TryGetValue(streamId, out var events) ? events.ToList() : []);

        public Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
            where TEvent : class, IAuditEventBase =>
            Task.FromResult<IReadOnlyList<TEvent>>(streams.TryGetValue(streamId, out var events) ? events.OfType<TEvent>().ToList() : []);

        public Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
            where TAggregate : class
            where TEvent : class, IAuditEventBase
        {
            streams[streamId] = [];
            foreach (var @event in events)
                AddEvent(streamId, @event);

            return Task.CompletedTask;
        }

        public Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default)
            where T : class, IAuditEventBase
        {
            AddEvent(streamId, @event);
            return Task.CompletedTask;
        }

        public Task AppendAsync(Guid streamId, IEnumerable<IAuditEventBase> events, CancellationToken cancellationToken = default)
        {
            foreach (var @event in events)
                AddEvent(streamId, @event);

            return Task.CompletedTask;
        }

        private void AddEvent(Guid streamId, IAuditEventBase @event)
        {
            if (!streams.TryGetValue(streamId, out var events))
            {
                events = [];
                streams[streamId] = events;
            }

            events.Add(@event);
            eventsById[@event.EventID.Value] = @event;
        }

        private static int CompareEventOrder(IEventBase left, IEventBase right)
        {
            var eventDateComparison = left.EventDateTime.Value.CompareTo(right.EventDateTime.Value);
            if (eventDateComparison != 0)
                return eventDateComparison;

            var auditDateComparison = left.AuditDateTime.Value.CompareTo(right.AuditDateTime.Value);
            if (auditDateComparison != 0)
                return auditDateComparison;

            return left.EventID.Value.CompareTo(right.EventID.Value);
        }
    }
}
