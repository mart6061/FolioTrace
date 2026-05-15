using System.Text.Json;
using System.Text.Json.Serialization;

namespace AILibrary.Types;

[JsonConverter(typeof(ISO2JsonConverter))]
public sealed record ISO2 : IType
{
    public string Value { get; init; } = null!; // initialize to satisfy the compiler

    // Regular constructor enforces rules
    public ISO2(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length != 2 || !IsAllUpperAsciiLetters(value))
            throw new ArgumentException("Value must be exactly 2 uppercase ASCII letters (A-Z).", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private ISO2() { }

    // Factory used by converter to create an instance without validation
    internal static ISO2 FromJson(string? value) => new ISO2 { Value = value! }; // value may be null when deserializing; suppress warning

    private static bool IsAllUpperAsciiLetters(string s) => (s[0] >= 'A' && s[0] <= 'Z') && (s[1] >= 'A' && s[1] <= 'Z');

    public static implicit operator string(ISO2 iso) => iso?.Value;

    public static implicit operator ISO2(string s) => new ISO2(s);

    public override string ToString() => Value;

    public string ToData() => Value;

    public string ToDetail() => $"{nameof(ISO2)}: {this}";
}

internal sealed class ISO2JsonConverter : JsonConverter<ISO2>
{
    public override ISO2? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for ISO2 value.");

        var s = reader.GetString();
        return ISO2.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, ISO2 value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
