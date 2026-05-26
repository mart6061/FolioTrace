using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Holding : IModel
{
    public required HoldingID HoldingID { get; init; }
    public required AccountID AccountID { get; init; }
    public required InstrumentID InstrumentID { get; init; }
    public required HoldingType HoldingType { get; init; }
    public HoldingNominalType? NominalType { get; init; }
    public required string Name { get; init; }
    public required bool Active { get; init; }
    public required bool Default { get; init; }
    public bool IncludeInValuation => HoldingType is not HoldingType.Nominal;
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Holding(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, HoldingType holdingType, HoldingNominalType? nominalType, string name, bool active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        HoldingID = holdingID ?? throw new ArgumentNullException(nameof(holdingID));
        AccountID = accountID ?? throw new ArgumentNullException(nameof(accountID));
        InstrumentID = instrumentID ?? throw new ArgumentNullException(nameof(instrumentID));
        HoldingType = holdingType;
        NominalType = nominalType;
        Name = name?.Trim() ?? string.Empty;
        Active = active;
        Default = isDefault;
        ValuationDateTime = valuationDateTime ?? throw new ArgumentNullException(nameof(valuationDateTime));
        AsOfDateTime = asOfDateTime ?? throw new ArgumentNullException(nameof(asOfDateTime));
        LastEventID = lastEventID ?? throw new ArgumentNullException(nameof(lastEventID));
        LastAuditDateTime = lastAuditDateTime ?? throw new ArgumentNullException(nameof(lastAuditDateTime));
    }

    [SetsRequiredMembers]
    public Holding(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, HoldingType holdingType, HoldingNominalType? nominalType, string name, bool active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime auditDateTime, EventID lastEventID)
        : this(holdingID, accountID, instrumentID, holdingType, nominalType, name, active, isDefault, valuationDateTime, auditDateTime, lastEventID, new LastAuditDateTime(auditDateTime.Value))
    {
    }

    public string ToData() => $"{HoldingID.ToData()}|{AccountID.ToData()}|{InstrumentID.ToData()}|{HoldingType}|{NominalType}|{Name}|{Active}|{Default}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Holding)}: {Name} ({HoldingID}, AccountID: {AccountID}, InstrumentID: {InstrumentID}, HoldingType: {HoldingType}, NominalType: {NominalType}, Active: {Active}, Default: {Default}, IncludeInValuation: {IncludeInValuation})";
}
