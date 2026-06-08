namespace FolioTrace.Types;

public static class AccountIDBuilder
{
    public static AccountID Create() => new(Guid.CreateGuid7());

    public static AccountID Create(Guid value) => new(value);
}
