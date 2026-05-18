using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Currency : IModel
{
    public required Alpha3 AlphabeticCode { get; init; }

    public required int NumericCode { get; init; }

    public required short DecimalPlace { get; init; }

    public required string Name { get; init; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required EventID LastEventID { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Currency(Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        AlphabeticCode = alphabeticCode;
        NumericCode = numericCode;
        DecimalPlace = decimalPlace;
        Name = name;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public Currency(Alpha3 alphabeticCode, int numericCode, short decimalPlace, string name, EventDateTime valuationDateTime, AuditDateTime auditDateTime, EventID lastEventID)
        : this(alphabeticCode, numericCode, decimalPlace, name, valuationDateTime, auditDateTime, lastEventID, ToLastAuditDateTime(auditDateTime))
    {
    }

    private static LastAuditDateTime ToLastAuditDateTime(AuditDateTime auditDateTime)
    {
        return new LastAuditDateTime(auditDateTime.Value);
    }

    public override string ToString() => AlphabeticCode.ToString();

    public string ToData() => $"{AlphabeticCode.ToData()}|{NumericCode}|{DecimalPlace}|{Name}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Currency)}: {Name} ({AlphabeticCode}, {NumericCode:D3}, {DecimalPlace}, ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastEventID: {LastEventID.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()})";
}
