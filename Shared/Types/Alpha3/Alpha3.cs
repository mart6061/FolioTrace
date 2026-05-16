using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(Alpha3JsonConverter))]
public sealed record Alpha3 : IType
{
    public string Value { get; init; }

    // Regular constructor enforces rules
    public Alpha3(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length != 3 || !IsAllUpperAsciiLetters(value))
            throw new ArgumentException("Value must be exactly 3 uppercase ASCII letters (A-Z).", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private Alpha3() { }

    // Factory used by converter to create an instance without validation
    internal static Alpha3 FromJson(string? value) => new Alpha3 { Value = value };

    private static bool IsAllUpperAsciiLetters(string s) =>
        (s[0] >= 'A' && s[0] <= 'Z') && (s[1] >= 'A' && s[1] <= 'Z') && (s[2] >= 'A' && s[2] <= 'Z');

    public static implicit operator string(Alpha3 iso) => iso?.Value;

    public static implicit operator Alpha3(string s) => new Alpha3(s);

    public override string ToString() => Value;

    public string ToData() => Value;

    public string ToDetail() => $"{nameof(Alpha3)}: {this}";
}

internal sealed class Alpha3JsonConverter : JsonConverter<Alpha3>
{
    public override Alpha3? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for Alpha3 value.");

        var s = reader.GetString();
        return Alpha3.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, Alpha3 value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
