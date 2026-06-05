using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(PriceJsonConverter))]
public sealed record Price : IType
{
    public decimal Amount { get; init; }

    public Price(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(amount));

        if (decimal.Round(amount, 8) != amount)
            throw new ArgumentException("Price can have at most 8 decimal places.", nameof(amount));

        Amount = amount;
    }

    [JsonConstructor]
    private Price() { }

    internal static Price FromJson(decimal amount) => new(amount);

    public override string ToString() => Amount.ToString("0.########");
}

internal sealed class PriceJsonConverter : JsonConverter<Price>
{
    public override Price? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Price.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, Price value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Amount);
}
