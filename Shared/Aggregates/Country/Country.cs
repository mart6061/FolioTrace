using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Country : IAggregate
{
    public required Alpha2 Alpha2 { get; init; }

    public required Alpha3 Alpha3 { get; init; }

    public required short Numeric { get; init; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    // Regular constructor enforces rules
    [JsonConstructor]
    [SetsRequiredMembers]
    public Country(Alpha2 alpha2, Alpha3 alpha3, short numeric, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, LastAuditDateTime lastAuditDateTime)
    {
        if (alpha2 is null)
            throw new ArgumentNullException(nameof(alpha2));

        if (alpha3 is null)
            throw new ArgumentNullException(nameof(alpha3));

        if (numeric < 0 || numeric > 999)
            throw new ArgumentException("Value must be between 0 and 999.", nameof(numeric));

        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        if (lastAuditDateTime is null)
            throw new ArgumentNullException(nameof(lastAuditDateTime));

        Alpha2 = alpha2;
        Alpha3 = alpha3;
        Numeric = numeric;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastAuditDateTime = lastAuditDateTime;
    }

    public override string ToString() => Alpha2.ToString();

    public string ToData() => $"{Alpha2.ToData()}|{Alpha3.ToData()}|{Numeric}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Country)}: (Alpha2: {Alpha2.ToDetail()}, Alpha3: {Alpha3.ToDetail()}, Numeric: {Numeric}, ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()})";
}
