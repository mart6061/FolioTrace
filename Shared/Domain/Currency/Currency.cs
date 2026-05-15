using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AILibrary.Types;

namespace AILibrary.Domain;

public sealed record Currency : IModel
{
    public required ISO3 AlphabeticCode { get; init; }

    public required int NumericCode { get; init; }

    public required short DecimalPlace { get; init; }

    public required string Name { get; init; }

    public required LastUpdatedDateTime LastUpdateDateTime { get; init; }

    // Regular constructor enforces rules
    [JsonConstructor]
    [SetsRequiredMembers]
    public Currency(ISO3 alphabeticCode, int numericCode, short decimalPlace, string name, LastUpdatedDateTime lastUpdateDateTime)
    {
        if (alphabeticCode is null)
            throw new ArgumentNullException(nameof(alphabeticCode));

        if (numericCode < 0 || numericCode > 999)
            throw new ArgumentException("Value must be between 0 and 999.", nameof(numericCode));

        if (decimalPlace < 0)
            throw new ArgumentException("Value must be zero or greater.", nameof(decimalPlace));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Value must not be null, empty, or whitespace.", nameof(name));

        if (lastUpdateDateTime is null)
            throw new ArgumentNullException(nameof(lastUpdateDateTime));

        AlphabeticCode = alphabeticCode;
        NumericCode = numericCode;
        DecimalPlace = decimalPlace;
        Name = name;
        LastUpdateDateTime = lastUpdateDateTime;
    }

    public override string ToString() => AlphabeticCode.ToString();

    public string ToData() => $"{AlphabeticCode.ToData()}|{NumericCode}|{DecimalPlace}|{Name}|{LastUpdateDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Currency)}: {Name} ({AlphabeticCode}, {NumericCode:D3}, {DecimalPlace}, LastUpdateDateTime: {LastUpdateDateTime.ToDetail()})";
}
