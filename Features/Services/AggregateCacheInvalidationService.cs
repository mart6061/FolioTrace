using FolioTrace.Common;

namespace Services;

public sealed class AggregateCacheInvalidationService(
    IEnumerable<IAggregateCacheInvalidator> invalidators,
    AggregateMaintenanceCoordinator aggregateMaintenanceCoordinator)
{
    private readonly IReadOnlyList<IAggregateCacheInvalidator> invalidators = invalidators.ToList();

    public int Invalidate(IAuditEventBase @event)
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        aggregateMaintenanceCoordinator.NotifyEventsCreated(1);

        var eventType = @event.GetType();

        return invalidators
            .Where(invalidator => invalidator.EventType.IsAssignableFrom(eventType))
            .Sum(invalidator => invalidator.Invalidate(@event));
    }

    public int Invalidate(IEnumerable<IAuditEventBase> events)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        return events.Sum(Invalidate);
    }
}
