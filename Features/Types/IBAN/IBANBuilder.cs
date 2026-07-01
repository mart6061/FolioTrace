using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class IBANBuilder
{
    public static IBAN Create(string value) => new(value);
}
