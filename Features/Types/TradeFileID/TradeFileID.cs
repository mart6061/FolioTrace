using System.Text.Json.Serialization;

namespace FolioTrace.Types;

public sealed record TradeFileID
{
    public Guid Value { get; init; }
    [JsonConstructor]
    public TradeFileID(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));
        Value = value;
    }
    public static implicit operator Guid(TradeFileID id) => id.Value;
    public static implicit operator TradeFileID(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
