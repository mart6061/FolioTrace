using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(BICJsonConverter))]
public sealed record BIC : IType
{
    public string Value { get; init; } = null!;

    public BIC(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        value = value.Trim().ToUpperInvariant();
        if (!IsValid(value))
            throw new ArgumentException("Value must be a valid BIC: 8 or 11 uppercase letters/digits with a 4-letter bank code and 2-letter country code.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private BIC() { }

    internal static BIC FromJson(string? value) => new(value!);

    private static bool IsValid(string value)
    {
        if (value.Length is not (8 or 11))
            return false;

        return value.Take(4).All(IsUpperAsciiLetter) &&
            IsUpperAsciiLetter(value[4]) &&
            IsUpperAsciiLetter(value[5]) &&
            IsUpperAsciiLetterOrDigit(value[6]) &&
            IsUpperAsciiLetterOrDigit(value[7]) &&
            (value.Length == 8 || value.Skip(8).All(IsUpperAsciiLetterOrDigit));
    }

    private static bool IsUpperAsciiLetter(char c) => c >= 'A' && c <= 'Z';

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static bool IsUpperAsciiLetterOrDigit(char c) => IsUpperAsciiLetter(c) || IsDigit(c);

    public static implicit operator string?(BIC? bic) => bic?.Value;

    public static implicit operator BIC(string s) => new(s);

    public override string ToString() => Value;

    public string ToData() => Value;

    public string ToDetail() => $"{nameof(BIC)}: {this}";
}

internal sealed class BICJsonConverter : JsonConverter<BIC>
{
    public override BIC? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for BIC value.");

        return BIC.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, BIC value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
