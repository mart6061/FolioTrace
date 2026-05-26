using System.Collections.Concurrent;
using System.Threading.Channels;
using FolioTrace.Aggregates;
using FolioTrace.Common;

namespace Services;

public sealed class AggregateUpdateNotificationService : IAggregateCacheInvalidator
{
    private readonly ConcurrentDictionary<Guid, Channel<object>> subscribers = new();
    private long publishedNotificationCount;
    private AggregateUpdateNotification? lastNotification;
    private BuildProgressNotification? lastBuildProgress;

    public Type EventType => typeof(IEventBase);

    public AggregateUpdateNotificationDiagnostics GetDiagnostics() =>
        new(
            subscribers.Count,
            Interlocked.Read(ref publishedNotificationCount),
            lastNotification?.NotificationType,
            lastNotification?.Kind,
            lastNotification?.EventID,
            lastNotification?.EventDateTime,
            lastNotification?.AuditDateTime,
            lastNotification?.Reason,
            lastBuildProgress?.Status == "Running" ? lastBuildProgress.BuildID : null,
            lastBuildProgress?.Status,
            lastBuildProgress?.Stage,
            lastBuildProgress?.UpdatedAtUtc);

    public AggregateUpdateNotificationSubscription Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        subscribers[id] = channel;

        return new AggregateUpdateNotificationSubscription(
            channel.Reader,
            () =>
            {
                if (subscribers.TryRemove(id, out var removed))
                    removed.Writer.TryComplete();
            });
    }

    public int Invalidate(IEventBase @event)
    {
        var notifications = CreateNotifications(@event);
        foreach (var notification in notifications)
            Publish(notification);

        return 0;
    }

    public void PublishAggregatesInvalidated(string reason) =>
        Publish(new AggregateUpdateNotification(
            "AggregatesInvalidated",
            "All",
            null,
            null,
            null,
            null,
            reason));

    public void PublishBuildProgress(BuildProgressNotification notification) =>
        Publish(notification);

    public void PublishAggregateMaintenance(AggregateMaintenanceNotification notification) =>
        Publish(notification);

    private void Publish(object notification)
    {
        if (notification is AggregateUpdateNotification aggregateNotification)
            lastNotification = aggregateNotification;
        else if (notification is BuildProgressNotification buildProgressNotification)
            lastBuildProgress = buildProgressNotification;

        Interlocked.Increment(ref publishedNotificationCount);

        foreach (var subscriber in subscribers.Values)
            subscriber.Writer.TryWrite(notification);
    }

    private static IReadOnlyList<AggregateUpdateNotification> CreateNotifications(IEventBase @event)
    {
        var kinds = AggregateKindsFor(@event).Distinct(StringComparer.Ordinal).ToArray();

        return kinds
            .Select(kind => new AggregateUpdateNotification(
                "AggregateUpdated",
                kind,
                @event.EventID.Value,
                @event.EventDateTime.Value,
                @event.AuditDateTime.Value,
                @event.EventDateTime.Value,
                @event.Reason))
            .ToArray();
    }

    private static IEnumerable<string> AggregateKindsFor(IEventBase @event) =>
        @event switch
        {
            IAccountEvent => ["Accounts"],
            ICountryEvent => ["Countries"],
            ICurrencyEvent => ["Currencies"],
            IFXEvent => ["FXs", "FXRates"],
            IFXRateEvent => ["FXRates"],
            IHoldingEvent => ["Holdings", "HoldingPositions"],
            IInstrumentEvent => ["Instruments", "InstrumentValues"],
            IInstrumentPriceEvent => ["InstrumentValues"],
            IInstrumentIncomeEvent => ["InstrumentValues"],
            ITransactionEvent => ["Transactions", "HoldingPositions"],
            _ => []
        };
}

public sealed record AggregateUpdateNotification(
    string NotificationType,
    string Kind,
    Guid? EventID,
    DateTime? EventDateTime,
    DateTime? AuditDateTime,
    DateTime? AffectedFrom,
    string Reason);

public sealed record AggregateUpdateNotificationDiagnostics(
    int ActiveSubscriberCount,
    long PublishedNotificationCount,
    string? LastNotificationType,
    string? LastKind,
    Guid? LastEventID,
    DateTime? LastEventDateTime,
    DateTime? LastAuditDateTime,
    string? LastReason,
    Guid? CurrentBuildID,
    string? LastBuildStatus,
    string? LastBuildStage,
    DateTime? LastBuildUpdatedAtUtc);

public sealed record BuildProgressNotification(
    string NotificationType,
    Guid BuildID,
    string Status,
    string Stage,
    string Message,
    int CompletedSteps,
    int TotalSteps,
    int CompletedEvents,
    int TotalEvents,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc,
    string? Error);

public sealed record AggregateMaintenanceNotification(
    string NotificationType,
    string Status,
    string? Trigger,
    bool Changed,
    DateTime UpdatedAtUtc);

public sealed class AggregateUpdateNotificationSubscription(
    ChannelReader<object> reader,
    Action unsubscribe) : IAsyncDisposable
{
    public ChannelReader<object> Reader { get; } = reader;

    public ValueTask DisposeAsync()
    {
        unsubscribe();
        return ValueTask.CompletedTask;
    }
}
