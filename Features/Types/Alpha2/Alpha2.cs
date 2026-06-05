using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(Alpha2JsonConverter))]
public sealed record Alpha2 : IType
{
    public string Value { get; init; } = null!; // initialize to satisfy the compiler

    // Regular constructor enforces rules
    public Alpha2(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length != 2 || !IsAllUpperAsciiLetters(value))
            throw new ArgumentException("Value must be exactly 2 uppercase ASCII letters (A-Z).", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private Alpha2() { }

    // Factory used by converter to create an instance without validation
    internal static Alpha2 FromJson(string? value) => new Alpha2 { Value = value! }; // value may be null when deserializing; suppress warning

    private static bool IsAllUpperAsciiLetters(string s) => (s[0] >= 'A' && s[0] <= 'Z') && (s[1] >= 'A' && s[1] <= 'Z');

    public static implicit operator string?(Alpha2? iso) => iso?.Value;

    public static implicit operator Alpha2(string s) => new Alpha2(s);

    public override string ToString() => Value;
}

internal sealed class Alpha2JsonConverter : JsonConverter<Alpha2>
{
    public override Alpha2? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for Alpha2 value.");

        var s = reader.GetString();
        return Alpha2.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, Alpha2 value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
