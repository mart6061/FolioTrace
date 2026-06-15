using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentIdentifier : IType
{
    public required InstrumentIdentifierType Type { get; init; }

    public required string Value { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentIdentifier(InstrumentIdentifierType type, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Identifier value is required.", nameof(value));

        var normalised = value.Trim();
        ValidateTypedValue(type, normalised);

        Type = type;
        Value = normalised;
    }

    public override string ToString() => $"{Type}: {Value}";

    private static void ValidateTypedValue(InstrumentIdentifierType type, string value)
    {
        switch (type)
        {
            case InstrumentIdentifierType.Sedol:
                _ = new Sedol(value);
                break;
            case InstrumentIdentifierType.ISIN:
                _ = new ISIN(value);
                break;
            case InstrumentIdentifierType.Ticker:
                _ = new Ticker(value);
                break;
            default:
                ValidateGeneric(value);
                break;
        }
    }

    private static string ValidateGeneric(string value)
    {
        if (value.Length > 32 || !value.All(character => character is >= 'A' and <= 'Z' || character is >= '0' and <= '9' || character is '.' or '-' or '_'))
            throw new ArgumentException("Identifier value must be 1 to 32 uppercase ASCII letters, digits, dots, hyphens, or underscores.", nameof(value));

        return value;
    }
}
