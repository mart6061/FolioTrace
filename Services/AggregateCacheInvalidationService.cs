using FolioTrace.Common;

namespace Services;

public sealed class AggregateCacheInvalidationService(IEnumerable<IAggregateCacheInvalidator> invalidators)
{
    private readonly Dictionary<Type, List<IAggregateCacheInvalidator>> invalidatorsByEventType = invalidators
        .GroupBy(invalidator => invalidator.EventType)
        .ToDictionary(group => group.Key, group => group.ToList());

    public int Invalidate(IEventBase @event)
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        return invalidatorsByEventType.TryGetValue(@event.GetType(), out var matchingInvalidators)
            ? matchingInvalidators.Sum(invalidator => invalidator.Invalidate(@event))
            : 0;
    }

    public int Invalidate(IEnumerable<IEventBase> events)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        return events.Sum(Invalidate);
    }
}
