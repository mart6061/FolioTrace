namespace FolioTrace.Common;

[AttributeUsage(AttributeTargets.Class)]
public sealed class EventClassAttribute : Attribute
{
    public EventClassTypeEnum EventType { get; init; } = EventClassTypeEnum.Created;
    public string? Description { get; init; }

}
