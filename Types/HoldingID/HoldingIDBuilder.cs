namespace FolioTrace.Types;

public static class HoldingIDBuilder
{
    public static HoldingID Create() => new(Guid.NewGuid());

    public static HoldingID Create(Guid value) => new(value);
}
