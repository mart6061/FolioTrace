using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class SortCodeBuilder
{
    public static SortCode Create(string value) => new(value);
}
