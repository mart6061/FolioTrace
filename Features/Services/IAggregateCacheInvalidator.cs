using FolioTrace.Common;

namespace Services;

public interface IAggregateCacheInvalidator
{
    Type EventType { get; }

    int Invalidate(IEventBase @event);
}
