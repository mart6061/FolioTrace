using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class BankAccountNumberBuilder
{
    public static BankAccountNumber Create(string value) => new(value);
}
