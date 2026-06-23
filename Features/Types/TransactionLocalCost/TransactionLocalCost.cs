using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(TransactionLocalCostJsonConverter))]
public sealed record TransactionLocalCost : IType
{
    public decimal Value { get; init; }

    public TransactionLocalCost(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("TransactionLocalCost must be zero or greater.", nameof(value));

        if (decimal.Round(value, 8) != value)
            throw new ArgumentException("TransactionLocalCost can have at most 8 decimal places.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private TransactionLocalCost() { }

    internal static TransactionLocalCost FromJson(decimal value) => new(value);

    public static implicit operator decimal(TransactionLocalCost localCost) => localCost?.Value ?? 0m;

    public static implicit operator TransactionLocalCost(decimal value) => new(value);

    public override string ToString() => Value.ToString("0.########");
}
