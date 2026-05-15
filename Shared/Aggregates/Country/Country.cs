using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AILibrary.Types;

namespace AILibrary.Aggregates;

public sealed record Country : IAggregate
{
    public required ISO2 ISO2 { get; init; }

    public required ISO3 ISO3 { get; init; }

    public required short Numeric { get; init; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    // Regular constructor enforces rules
    [JsonConstructor]
    [SetsRequiredMembers]
    public Country(ISO2 iso2, ISO3 iso3, short numeric, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, LastAuditDateTime lastAuditDateTime)
    {
        if (iso2 is null)
            throw new ArgumentNullException(nameof(iso2));

        if (iso3 is null)
            throw new ArgumentNullException(nameof(iso3));

        if (numeric < 0 || numeric > 999)
            throw new ArgumentException("Value must be between 0 and 999.", nameof(numeric));

        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        if (lastAuditDateTime is null)
            throw new ArgumentNullException(nameof(lastAuditDateTime));

        ISO2 = iso2;
        ISO3 = iso3;
        Numeric = numeric;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastAuditDateTime = lastAuditDateTime;
    }

    public override string ToString() => ISO2.ToString();

    public string ToData() => $"{ISO2.ToData()}|{ISO3.ToData()}|{Numeric}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Country)}: (ISO2: {ISO2.ToDetail()}, ISO3: {ISO3.ToDetail()}, Numeric: {Numeric}, ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()})";
}
