namespace FolioTrace.Common;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class DetailsAttribute(string details) : Attribute
{
    public string Details { get; } = details;
}
