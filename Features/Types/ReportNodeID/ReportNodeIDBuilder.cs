using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class ReportNodeIDBuilder
{
    public static ReportNodeID Create() => new(Guid.CreateGuid7());

    public static ReportNodeID Create(Guid value) => new(value);
}
