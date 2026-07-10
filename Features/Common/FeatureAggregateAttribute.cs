namespace FolioTrace.Common;

[AttributeUsage(AttributeTargets.Class)]
public sealed class FeatureAggregateAttribute : Attribute
{
    public string? Description { get; init; }
}
