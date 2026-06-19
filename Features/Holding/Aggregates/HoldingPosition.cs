using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingPosition : IModel
{
    public required HoldingID HoldingID { get; init; }
    public required AccountID AccountID { get; init; }
    public required string AccountName { get; init; }
    public required InstrumentID InstrumentID { get; init; }
    public required string InstrumentName { get; init; }
    public required string HoldingKind { get; init; }
    public required string Name { get; init; }
    public required Active Active { get; init; }
    public required bool Default { get; init; }
    public required bool IncludeInValuation { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal BookCost { get; init; }
    public required EventDateTime ValuationDateTime { get; init; }
    public required HoldingDateBasis HoldingDateBasis { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingPosition(HoldingBase holding, string accountName, string instrumentName, decimal quantity, decimal bookCost, EventDateTime valuationDateTime, HoldingDateBasis holdingDateBasis, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        if (holding is null)
            throw new ArgumentNullException(nameof(holding));

        HoldingID = holding.HoldingID;
        AccountID = holding.AccountID;
        AccountName = accountName;
        InstrumentID = holding.InstrumentID;
        InstrumentName = instrumentName;
        HoldingKind = holding.GetHoldingKindName();
        Name = holding.Name;
        Active = holding.Active;
        Default = holding.Default;
        IncludeInValuation = holding.IncludeInValuation;
        Quantity = quantity;
        BookCost = bookCost;
        ValuationDateTime = valuationDateTime;
        HoldingDateBasis = holdingDateBasis;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }
}
