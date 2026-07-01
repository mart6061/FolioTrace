using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class EventSetIDBuilder
{
    public static EventSetID Create() => new(Guid.CreateGuid7());

    public static EventSetID Create(Guid value) => new(value);
}
