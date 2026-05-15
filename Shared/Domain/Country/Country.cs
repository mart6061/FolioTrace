using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AILibrary.Types;

namespace AILibrary.Domain;

public sealed record Country : IModel
{
    public required ISO2 ISO2 { get; init; }

    public required ISO3 ISO3 { get; init; }

    public required short Numeric { get; init; }

    public required LastUpdatedDateTime LastUpdateDateTime { get; init; }

    // Regular constructor enforces rules
    [JsonConstructor]
    [SetsRequiredMembers]
    public Country(ISO2 iso2, ISO3 iso3, short numeric, LastUpdatedDateTime lastUpdateDateTime)
    {
        if (iso2 is null)
            throw new ArgumentNullException(nameof(iso2));

        if (iso3 is null)
            throw new ArgumentNullException(nameof(iso3));

        if (numeric < 0 || numeric > 999)
            throw new ArgumentException("Value must be between 0 and 999.", nameof(numeric));

        if (lastUpdateDateTime is null)
            throw new ArgumentNullException(nameof(lastUpdateDateTime));

        ISO2 = iso2;
        ISO3 = iso3;
        Numeric = numeric;
        LastUpdateDateTime = lastUpdateDateTime;
    }

    public override string ToString() => ISO2.ToString();

    public string ToData() => $"{ISO2.ToData()}|{ISO3.ToData()}|{Numeric}|{LastUpdateDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Country)}: (ISO2: {ISO2.ToDetail()}, ISO3: {ISO3.ToDetail()}, Numeric: {Numeric}, LastUpdateDateTime: {LastUpdateDateTime.ToDetail()})";
}

