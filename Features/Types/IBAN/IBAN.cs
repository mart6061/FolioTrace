using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(IBANJsonConverter))]
public sealed record IBAN : IType
{
    public string Value { get; init; } = null!;

    public IBAN(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        value = value.Replace(" ", string.Empty).Trim().ToUpperInvariant();
        if (!IsValid(value))
            throw new ArgumentException("Value must be a valid IBAN: 2-letter country code, 2 check digits, up to 30 letters/digits, and a valid mod-97 checksum.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private IBAN() { }

    internal static IBAN FromJson(string? value) => new(value!);

    private static bool IsValid(string value)
    {
        if (value.Length is < 15 or > 34)
            return false;

        if (!IsUpperAsciiLetter(value[0]) || !IsUpperAsciiLetter(value[1]) || !IsDigit(value[2]) || !IsDigit(value[3]))
            return false;

        if (!value.All(IsUpperAsciiLetterOrDigit))
            return false;

        return HasValidCheckDigits(value);
    }

    private static bool HasValidCheckDigits(string value)
    {
        var remainder = 0;
        foreach (var c in value[4..].Concat(value[..4]))
        {
            if (IsDigit(c))
            {
                remainder = (remainder * 10 + c - '0') % 97;
                continue;
            }

            var expanded = c - 'A' + 10;
            remainder = (remainder * 100 + expanded) % 97;
        }

        return remainder == 1;
    }

    private static bool IsUpperAsciiLetter(char c) => c >= 'A' && c <= 'Z';

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static bool IsUpperAsciiLetterOrDigit(char c) => IsUpperAsciiLetter(c) || IsDigit(c);

    public static implicit operator string?(IBAN? iban) => iban?.Value;

    public static implicit operator IBAN(string s) => new(s);

    public override string ToString() => Value;
}
