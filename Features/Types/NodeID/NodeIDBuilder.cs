using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class NodeIDBuilder
{
    public static NodeID Create() => new(Guid.CreateGuid7());

    public static NodeID Create(Guid value) => new(value);
}
