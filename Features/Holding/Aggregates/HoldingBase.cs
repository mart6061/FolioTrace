using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(HoldingPositionMemo), nameof(HoldingPositionMemo))]
[JsonDerivedType(typeof(HoldingPositionCash), nameof(HoldingPositionCash))]
[JsonDerivedType(typeof(HoldingPositionAsset), nameof(HoldingPositionAsset))]
[JsonDerivedType(typeof(HoldingCashDebt), nameof(HoldingCashDebt))]
[JsonDerivedType(typeof(HoldingCashInvestable), nameof(HoldingCashInvestable))]
[JsonDerivedType(typeof(HoldingCashNonInvestable), nameof(HoldingCashNonInvestable))]
[JsonDerivedType(typeof(HoldingNominalInflow), nameof(HoldingNominalInflow))]
[JsonDerivedType(typeof(HoldingNominalOutflow), nameof(HoldingNominalOutflow))]
[JsonDerivedType(typeof(HoldingNominalInSpecieIn), nameof(HoldingNominalInSpecieIn))]
[JsonDerivedType(typeof(HoldingNominalInSpecieOut), nameof(HoldingNominalInSpecieOut))]
[JsonDerivedType(typeof(HoldingNominalFeesCustodian), nameof(HoldingNominalFeesCustodian))]
[JsonDerivedType(typeof(HoldingNominalFeesAdministrator), nameof(HoldingNominalFeesAdministrator))]
[JsonDerivedType(typeof(HoldingNominalFeesBank), nameof(HoldingNominalFeesBank))]
[JsonDerivedType(typeof(HoldingNominalIncome), nameof(HoldingNominalIncome))]
[JsonDerivedType(typeof(HoldingNominalInterest), nameof(HoldingNominalInterest))]
public abstract record HoldingBase : IModel
{
    public required HoldingID HoldingID { get; init; }
    public required AccountID AccountID { get; init; }
    public required InstrumentID InstrumentID { get; init; }
    public required string Name { get; init; }
    public required Active Active { get; init; }
    public required bool Default { get; init; }
    public bool IncludeInValuation => this is IHoldingPosition;
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [SetsRequiredMembers]
    protected HoldingBase(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        HoldingID = holdingID ?? throw new ArgumentNullException(nameof(holdingID));
        AccountID = accountID ?? throw new ArgumentNullException(nameof(accountID));
        InstrumentID = instrumentID ?? throw new ArgumentNullException(nameof(instrumentID));
        Name = name?.Trim() ?? string.Empty;
        Active = active;
        Default = isDefault;
        ValuationDateTime = valuationDateTime ?? throw new ArgumentNullException(nameof(valuationDateTime));
        AsOfDateTime = asOfDateTime ?? throw new ArgumentNullException(nameof(asOfDateTime));
        LastEventID = lastEventID ?? throw new ArgumentNullException(nameof(lastEventID));
        LastAuditDateTime = lastAuditDateTime ?? throw new ArgumentNullException(nameof(lastAuditDateTime));
    }
}
