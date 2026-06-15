using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(LegalEntityIdentifierJsonConverter))]
public sealed record LegalEntityIdentifier : IType
{
    public string Value { get; init; } = null!;

    public LegalEntityIdentifier(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        value = value.Trim().ToUpperInvariant();
        if (!IsValid(value))
            throw new ArgumentException("Value must be a valid 20-character Legal Entity Identifier.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private LegalEntityIdentifier() { }

    internal static LegalEntityIdentifier FromJson(string? value) => new(value!);

    private static bool IsValid(string value)
    {
        if (value.Length != 20)
            return false;

        return value.All(IsUpperAsciiLetterOrDigit);
    }

    private static bool IsUpperAsciiLetterOrDigit(char c) =>
        c is >= 'A' and <= 'Z' || c is >= '0' and <= '9';

    public static implicit operator string?(LegalEntityIdentifier? lei) => lei?.Value;

    public static implicit operator LegalEntityIdentifier(string value) => new(value);

    public override string ToString() => Value;
}
