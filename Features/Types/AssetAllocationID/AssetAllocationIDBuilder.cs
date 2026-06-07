namespace FolioTrace.Types;

public static class AssetAllocationIDBuilder
{
    public static AssetAllocationID Create() => new(Guid.CreateGuid7());

    public static AssetAllocationID Create(Guid value) => new(value);
}
