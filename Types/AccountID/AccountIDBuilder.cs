namespace FolioTrace.Types;

public static class AccountIDBuilder
{
    public static AccountID Create() => new(Guid.NewGuid());

    public static AccountID Create(Guid value) => new(value);
}
