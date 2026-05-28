namespace FolioTrace.Types;

public static class IBANBuilder
{
    public static IBAN Create(string value) => new(value);
}
