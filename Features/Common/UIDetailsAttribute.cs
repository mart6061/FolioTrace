namespace FolioTrace.Common;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class UIDetailsAttribute(string Short, string Long, int Order) : Attribute
{
    public string Short { get; } = Short;

    public string Long { get; } = Long;

    public int Order { get; } = Order;
}
