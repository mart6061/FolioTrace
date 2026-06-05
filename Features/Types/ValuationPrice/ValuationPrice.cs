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

    public static implicit operator decimal?(ValuationPrice price) => price?.Amount;

    public static implicit operator ValuationPrice(decimal? amount) => new(amount);

    public override string ToString() => $"{Amount:0.########}";
}
