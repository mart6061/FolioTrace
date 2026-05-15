using System.Text.Json;
using System.Text.Json.Serialization;

namespace AILibrary.Types;

[JsonConverter(typeof(ISINJsonConverter))]
public sealed record ISIN : IType
{
    public string Value { get; init; } = null!; // initialize to satisfy the compiler

    // Regular constructor enforces rules
    public ISIN(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (!IsValidISIN(value))
            throw new ArgumentException("Value must be a valid ISIN identifier: 2 uppercase ASCII letters, 9 uppercase ASCII letters or digits, and a valid check digit.", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private ISIN() { }

    // Factory used by converter to create an instance without validation
    internal static ISIN FromJson(string? value) => new ISIN { Value = value! }; // value may be null when deserializing; suppress warning

    private static bool IsValidISIN(string s)
    {
        if (s.Length != 12)
            return false;

        if (!IsUpperAsciiLetter(s[0]) || !IsUpperAsciiLetter(s[1]))
            return false;

        for (var i = 2; i < 11; i++)
        {
            if (!IsUpperAsciiLetterOrDigit(s[i]))
                return false;
        }

        if (!IsDigit(s[11]))
            return false;

        return HasValidCheckDigit(s);
    }

    private static bool HasValidCheckDigit(string s)
    {
        var sum = 0;
        var doubleDigit = false;

        for (var i = s.Length - 1; i >= 0; i--)
        {
            var c = s[i];

            if (IsDigit(c))
            {
                sum += SumLuhnDigit(c - '0', doubleDigit);
                doubleDigit = !doubleDigit;
                continue;
            }

            var expanded = c - 'A' + 10;
            sum += SumLuhnDigit(expanded % 10, doubleDigit);
            doubleDigit = !doubleDigit;
            sum += SumLuhnDigit(expanded / 10, doubleDigit);
            doubleDigit = !doubleDigit;
        }

        return sum % 10 == 0;
    }

    private static int SumLuhnDigit(int digit, bool doubleDigit)
    {
        if (!doubleDigit)
            return digit;

        var doubled = digit * 2;
        return doubled > 9 ? doubled - 9 : doubled;
    }

    private static bool IsUpperAsciiLetter(char c) => c >= 'A' && c <= 'Z';

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static bool IsUpperAsciiLetterOrDigit(char c) => IsUpperAsciiLetter(c) || IsDigit(c);

    public static implicit operator string(ISIN isin) => isin?.Value;

    public static implicit operator ISIN(string s) => new ISIN(s);

    public override string ToString() => Value;

    public string ToData() => Value;

    public string ToDetail() => $"{nameof(ISIN)}: {this}";
}

internal sealed class ISINJsonConverter : JsonConverter<ISIN>
{
    public override ISIN? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for ISIN value.");

        var s = reader.GetString();
        return ISIN.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, ISIN value, JsonSerializerOptions options)
    {
        if (value is null || value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
