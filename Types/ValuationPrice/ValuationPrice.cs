using System.Text.Json.Serialization;

namespace FolioTrace.Types;

public sealed record ValuationPrice : IType
{
    public decimal? Amount { get; init; }

    [JsonConstructor]
    public ValuationPrice(decimal? amount)
    {
        if (amount.HasValue && decimal.Round(amount.Value, 8) != amount.Value)
            throw new ArgumentException("ValuationPrice can have at most 8 decimal places.", nameof(amount));

        Amount = amount;
    }

    public override string ToString() => $"{Amount:0.########}";

    public string ToData() => $"{Amount:0.########}";

    public string ToDetail() => $"{nameof(ValuationPrice)}: {this}";
}
