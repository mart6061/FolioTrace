using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class BICBuilder
{
    public static BIC Create(string value) => new(value);
}
