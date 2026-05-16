using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Currency : IAggregate
{
    public required Alpha3 AlphabeticCode { get; init; }

    public required int NumericCode { get; init; }

    public required short DecimalPlace { get; init; }

    public required string Name { get; init; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    // Regular constructor enforces rules
    [JsonConstructor]
    [SetsRequiredMembers]
    public Currency(Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, LastAuditDateTime lastAuditDateTime)
    {
        if (alphabeticCode is null)
            throw new ArgumentNullException(nameof(alphabeticCode));


        if (numericCode < 0 || numericCode > 999)
            throw new ArgumentException("Value must be between 0 and 999.", nameof(numericCode));

        if (decimalPlace < 0)
            throw new ArgumentException("Value must be zero or greater.", nameof(decimalPlace));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Value must not be null, empty, or whitespace.", nameof(name));

        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        if (lastAuditDateTime is null)
            throw new ArgumentNullException(nameof(lastAuditDateTime));

        AlphabeticCode = alphabeticCode;
        NumericCode = numericCode;
        DecimalPlace = decimalPlace;
        Name = name;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public Currency(Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name, EventDateTime valuationDateTime, AuditDateTime auditDateTime)
        : this(alphabeticCode, numericCode, decimalPlace, name, valuationDateTime, auditDateTime, ToLastAuditDateTime(auditDateTime))
    {
    }

    private static LastAuditDateTime ToLastAuditDateTime(AuditDateTime auditDateTime)
    {
        if (auditDateTime is null)
            throw new ArgumentNullException(nameof(auditDateTime));

        return new LastAuditDateTime(auditDateTime.Value);
    }

    public override string ToString() => AlphabeticCode.ToString();

    public string ToData() => $"{AlphabeticCode.ToData()}|{NumericCode}|{DecimalPlace}|{Name}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Currency)}: {Name} ({AlphabeticCode}, {NumericCode:D3}, {DecimalPlace}, ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()})";
}
