using System.Text.Json;
using System.Text.Json.Serialization;

namespace AILibrary.Types;

[JsonConverter(typeof(ISO3JsonConverter))]
public sealed record ISO3 : IType
{
    public string Value { get; init; }

    // Regular constructor enforces rules
    public ISO3(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length != 3 || !IsAllUpperAsciiLetters(value))
            throw new ArgumentException("Value must be exactly 3 uppercase ASCII letters (A-Z).", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private ISO3() { }

    // Factory used by converter to create an instance without validation
    internal static ISO3 FromJson(string? value) => new ISO3 { Value = value };

    private static bool IsAllUpperAsciiLetters(string s) =>
        (s[0] >= 'A' && s[0] <= 'Z') && (s[1] >= 'A' && s[1] <= 'Z') && (s[2] >= 'A' && s[2] <= 'Z');

    public static implicit operator string(ISO3 iso) => iso?.Value;

    public static implicit operator ISO3(string s) => new ISO3(s);

    public override string ToString() => Value;

    public string ToData() => Value;

    public string ToDetail() => $"{nameof(ISO3)}: {this}";
}

internal sealed class ISO3JsonConverter : JsonConverter<ISO3>
{
    public override ISO3? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for ISO3 value.");

        var s = reader.GetString();
        return ISO3.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, ISO3 value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
