using FolioTrace.Common;

namespace Services;

public sealed class AggregateCacheInvalidator<TEvent>(Func<TEvent, int> invalidate) : IAggregateCacheInvalidator<TEvent>
    where TEvent : class, IAuditEventBase
{
    public Type EventType => typeof(TEvent);

    public int Invalidate(IAuditEventBase @event) =>
        @event is TEvent typedEvent ? Invalidate(typedEvent) : 0;

    public int Invalidate(TEvent @event) => invalidate(@event);
}
