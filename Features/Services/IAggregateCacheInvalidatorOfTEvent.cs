using FolioTrace.Common;

namespace Services;

public interface IAggregateCacheInvalidator<in TEvent> : IAggregateCacheInvalidator
    where TEvent : class, IEventBase
{
    int Invalidate(TEvent @event);
}
