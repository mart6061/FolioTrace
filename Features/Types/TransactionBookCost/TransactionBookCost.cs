using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(TransactionBookCostJsonConverter))]
public sealed record TransactionBookCost : IType
{
    public decimal Value { get; init; }

    public TransactionBookCost(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("TransactionBookCost must be zero or greater.", nameof(value));

        if (decimal.Round(value, 8) != value)
            throw new ArgumentException("TransactionBookCost can have at most 8 decimal places.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private TransactionBookCost() { }

    internal static TransactionBookCost FromJson(decimal value) => new(value);

    public static implicit operator decimal(TransactionBookCost bookCost) => bookCost?.Value ?? 0m;

    public static implicit operator TransactionBookCost(decimal value) => new(value);

    public override string ToString() => Value.ToString("0.########");
}

internal sealed class TransactionBookCostJsonConverter : JsonConverter<TransactionBookCost>
{
    public override TransactionBookCost? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : TransactionBookCost.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, TransactionBookCost value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
