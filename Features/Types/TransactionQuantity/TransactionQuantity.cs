using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(TransactionQuantityJsonConverter))]
public sealed record TransactionQuantity : IType
{
    public decimal Value { get; init; }

    public TransactionQuantity(decimal value)
    {
        if (value <= 0)
            throw new ArgumentException("TransactionQuantity must be greater than zero.", nameof(value));

        if (decimal.Round(value, 8) != value)
            throw new ArgumentException("TransactionQuantity can have at most 8 decimal places.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private TransactionQuantity() { }

    internal static TransactionQuantity FromJson(decimal value) => new(value);

    public static implicit operator decimal(TransactionQuantity quantity) => quantity?.Value ?? 0m;

    public static implicit operator TransactionQuantity(decimal value) => new(value);

    public override string ToString() => Value.ToString("0.########");
}
