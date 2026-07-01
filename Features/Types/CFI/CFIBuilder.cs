using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class CFIBuilder
{
    public static CFI Create(string value) => new(value);
}
