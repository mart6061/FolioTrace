namespace FolioTrace.Types;

public static class EventSetIDBuilder
{
    public static EventSetID Create() => new(Guid.NewGuid());

    public static EventSetID Create(Guid value) => new(value);
}
