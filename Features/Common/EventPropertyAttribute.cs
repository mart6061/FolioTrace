namespace FolioTrace.Common;

[AttributeUsage(AttributeTargets.Property)]
public sealed class EventPropertyAttribute : Attribute
{
    public string? Description { get; init; }

    public int Order { get; init; } = int.MaxValue;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class EventClassAttribute : Attribute
{
    public EventClassTypeEnum EventType { get; init; } = EventClassTypeEnum.Created;
    public string? Description { get; init; }

}

public enum EventClassTypeEnum
{
    Created = 0,
    Modified = 1,
    Cancelled = 2,
    System = 3,
    Authentication = 4,
    Transaction = 5,
}
