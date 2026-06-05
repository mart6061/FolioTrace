using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(CurrencyPairJsonConverter))]
public sealed record CurrencyPair : IType
{
    public Alpha3 BaseCurrency { get; init; } = null!;

    public Alpha3 QuoteCurrency { get; init; } = null!;

    public string Value => $"{BaseCurrency}{QuoteCurrency}";

    public string DisplayValue => $"{BaseCurrency}/{QuoteCurrency}";

    public CurrencyPair(Alpha3 baseCurrency, Alpha3 quoteCurrency)
    {
        if (baseCurrency is null)
            throw new ArgumentNullException(nameof(baseCurrency));

        if (quoteCurrency is null)
            throw new ArgumentNullException(nameof(quoteCurrency));

        if (baseCurrency == quoteCurrency)
            throw new ArgumentException("Base and quote currencies must be different.", nameof(quoteCurrency));

        BaseCurrency = baseCurrency;
        QuoteCurrency = quoteCurrency;
    }

    [JsonConstructor]
    private CurrencyPair()
    {
    }

    internal static CurrencyPair FromJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency pair is required.", nameof(value));

        var normalized = value.Replace("/", string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length != 6)
            throw new ArgumentException("Currency pair must use six letters, for example EURGBP.", nameof(value));

        return new CurrencyPair(normalized[..3], normalized[3..]);
    }

    public override string ToString() => Value;
}

internal sealed class CurrencyPairJsonConverter : JsonConverter<CurrencyPair>
{
    public override CurrencyPair? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for CurrencyPair value.");

        return CurrencyPair.FromJson(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, CurrencyPair value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
