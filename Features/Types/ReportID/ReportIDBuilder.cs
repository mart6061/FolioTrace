using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class ReportIDBuilder
{
    public static ReportID Create() => new(Guid.CreateGuid7());

    public static ReportID Create(Guid value) => new(value);
}
