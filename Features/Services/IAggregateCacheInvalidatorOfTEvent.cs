using FolioTrace.Common;

namespace Services;

public interface IAggregateCacheInvalidator<in TEvent> : IAggregateCacheInvalidator
    where TEvent : class, IAuditEventBase
{
    int Invalidate(TEvent @event);
}
