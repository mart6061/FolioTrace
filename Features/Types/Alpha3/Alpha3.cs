using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(Alpha3JsonConverter))]
public sealed record Alpha3 : IType
{
    public string Value { get; init; } = null!;

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
    internal static Alpha3 FromJson(string? value) => new Alpha3 { Value = value! };

    private static bool IsAllUpperAsciiLetters(string s) =>
        (s[0] >= 'A' && s[0] <= 'Z') && (s[1] >= 'A' && s[1] <= 'Z') && (s[2] >= 'A' && s[2] <= 'Z');

    public static implicit operator string?(Alpha3? iso) => iso?.Value;

    public static implicit operator Alpha3(string s) => new Alpha3(s);

    public override string ToString() => Value;
}
