using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(TickerJsonConverter))]
public sealed record Ticker : IType
{
    public string Value { get; init; } = null!; // initialize to satisfy the compiler

    // Regular constructor enforces rules
    public Ticker(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length < 1 || value.Length > 20 || !IsValidTicker(value))
            throw new ArgumentException("Value must be 1 to 20 uppercase ASCII letters, digits, dots, or hyphens.", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private Ticker() { }

    // Factory used by converter to create an instance without validation
    internal static Ticker FromJson(string? value) => new Ticker { Value = value! }; // value may be null when deserializing; suppress warning

    private static bool IsValidTicker(string s)
    {
        foreach (var c in s)
        {
            if ((c < 'A' || c > 'Z') && (c < '0' || c > '9') && c != '.' && c != '-')
                return false;
        }

        return true;
    }

    public static implicit operator string?(Ticker? ticker) => ticker?.Value;

    public static implicit operator Ticker(string s) => new Ticker(s);

    public override string ToString() => Value;
}
