namespace FolioTrace.Types;

public static class HoldingIDBuilder
{
    public static HoldingID Create() => new(Guid.CreateGuid7());

    public static HoldingID Create(Guid value) => new(value);
}
