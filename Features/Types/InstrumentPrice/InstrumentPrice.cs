using System.Text.Json.Serialization;

namespace FolioTrace.Types;

public sealed record InstrumentPrice : IType
{
    public decimal? Amount { get; init; }

    [JsonConstructor]
    public InstrumentPrice(decimal? amount)
    {
        if (amount.HasValue && decimal.Round(amount.Value, 8) != amount.Value)
            throw new ArgumentException("InstrumentPrice can have at most 8 decimal places.", nameof(amount));

        Amount = amount;
    }

    public override string ToString() => $"{Amount:0.########}";

    public string ToData() => $"{Amount:0.########}";

    public string ToDetail() => $"{nameof(InstrumentPrice)}: {this}";
}
