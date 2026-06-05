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

    public static implicit operator decimal?(InstrumentPrice price) => price?.Amount;

    public static implicit operator InstrumentPrice(decimal? amount) => new(amount);

    public override string ToString() => $"{Amount:0.########}";
}
