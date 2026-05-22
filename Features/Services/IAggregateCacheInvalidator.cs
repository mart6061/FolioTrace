using FolioTrace.Common;

namespace Services;

public interface IAggregateCacheInvalidator
{
    Type EventType { get; }

    int Invalidate(IEventBase @event);
}

public interface IAggregateCacheInvalidator<in TEvent> : IAggregateCacheInvalidator
    where TEvent : class, IEventBase
{
    int Invalidate(TEvent @event);
}
