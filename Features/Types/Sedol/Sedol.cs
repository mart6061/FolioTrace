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

    public static implicit operator string?(Sedol? sedol) => sedol?.Value;

    public static implicit operator Sedol(string s) => new Sedol(s);

    public override string ToString() => Value;
}
