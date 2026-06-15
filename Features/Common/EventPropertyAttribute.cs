namespace FolioTrace.Common;

[AttributeUsage(AttributeTargets.Property)]
public sealed class EventPropertyAttribute : Attribute
{
    public string? Description { get; init; }

    public int Order { get; init; } = int.MaxValue;
}
