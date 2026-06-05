using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(ExchangeJsonConverter))]
public sealed record Exchange : IType
{
    public string Value { get; init; } = null!;

    public Exchange(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Exchange is required.", nameof(value));

        var normalised = value.Trim();
        if (normalised.Length is < 2 or > 12 || !normalised.All(character => character is >= 'A' and <= 'Z' || character is >= '0' and <= '9' || character == '-' || character == '.'))
            throw new ArgumentException("Exchange must be 2 to 12 uppercase ASCII letters, digits, dots, or hyphens.", nameof(value));

        Value = normalised;
    }

    [JsonConstructor]
    private Exchange() { }

    internal static Exchange FromJson(string? value) => new() { Value = value! };

    public static implicit operator string?(Exchange? exchange) => exchange?.Value;

    public static implicit operator Exchange(string value) => new(value);

    public override string ToString() => Value;
}

public static class ExchangeBuilder
{
    public static Exchange Create(string value) => new(value);
}

internal sealed class ExchangeJsonConverter : JsonConverter<Exchange>
{
    public override Exchange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for Exchange value.");

        return Exchange.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, Exchange value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
