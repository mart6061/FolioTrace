using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(SedolJsonConverter))]
public sealed record Sedol : IType
{
    public string Value { get; init; } = null!; // initialize to satisfy the compiler

    // Regular constructor enforces rules
    public Sedol(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length != 7 || !IsAllUpperAsciiLettersOrDigits(value))
            throw new ArgumentException("Value must be exactly 7 uppercase ASCII letters or digits (A-Z, 0-9).", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private Sedol() { }

    // Factory used by converter to create an instance without validation
    internal static Sedol FromJson(string? value) => new Sedol { Value = value! }; // value may be null when deserializing; suppress warning

    private static bool IsAllUpperAsciiLettersOrDigits(string s)
    {
        foreach (var c in s)
        {
            if ((c < 'A' || c > 'Z') && (c < '0' || c > '9'))
                return false;
        }

        return true;
    }

    public static implicit operator string(Sedol sedol) => sedol?.Value;

    public static implicit operator Sedol(string s) => new Sedol(s);

    public override string ToString() => Value;

    public string ToData() => Value;

    public string ToDetail() => $"{nameof(Sedol)}: {this}";
}

internal sealed class SedolJsonConverter : JsonConverter<Sedol>
{
    public override Sedol? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for Sedol value.");

        var s = reader.GetString();
        return Sedol.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, Sedol value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
