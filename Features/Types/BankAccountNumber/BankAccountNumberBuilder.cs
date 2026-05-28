namespace FolioTrace.Types;

public static class BankAccountNumberBuilder
{
    public static BankAccountNumber Create(string value) => new(value);
}
