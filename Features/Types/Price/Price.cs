using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(PriceJsonConverter))]
public record Price : IType
{
    public decimal Amount { get; init; }

    public Price(decimal amount)
        : this(amount, allowZero: false)
    {
    }

    protected Price(decimal amount, bool allowZero)
    {
        if (allowZero)
        {
            if (amount < 0)
                throw new ArgumentException("Price must be zero or greater.", nameof(amount));
        }
        else if (amount <= 0)
        {
            throw new ArgumentException("Price must be greater than zero.", nameof(amount));
        }

        if (decimal.Round(amount, 8) != amount)
            throw new ArgumentException("Price can have at most 8 decimal places.", nameof(amount));

        Amount = amount;
    }

    [JsonConstructor]
    protected Price() { }

    internal static Price FromJson(decimal amount) => new(amount);

    public static implicit operator decimal(Price price) => price?.Amount ?? 0m;

    public override string ToString() => Amount.ToString("0.########");
}
