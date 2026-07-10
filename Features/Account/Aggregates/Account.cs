using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Account : IModel
{
    public required AccountID AccountID { get; init; }
    public required string Name { get; init; }
    public required string FormalName { get; init; }
    public required Alpha3 BookCurrency { get; init; }
    public required ProfitLossMethod BookCostBasis { get; init; }
    public required List<InstrumentIdentifier> Identifiers { get; init; }
    public required Active Active { get; init; }
    public required DisplayOrder DisplayOrder { get; init; }
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Account(AccountID accountID, string name, string formalName, Alpha3 bookCurrency, ProfitLossMethod? bookCostBasis, List<InstrumentIdentifier>? identifiers, Active active, DisplayOrder? displayOrder, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        AccountID = accountID;
        Name = name;
        FormalName = formalName;
        BookCurrency = bookCurrency;
        BookCostBasis = bookCostBasis ?? ProfitLossMethod.FIFO;
        Identifiers = identifiers ?? [];
        Active = active;
        DisplayOrder = displayOrder ?? new DisplayOrder(0);
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public Account(AccountID accountID, string name, string formalName, Alpha3 bookCurrency, ProfitLossMethod bookCostBasis, List<InstrumentIdentifier>? identifiers, Active active, DisplayOrder displayOrder, EventDateTime valuationDateTime, AuditDateTime auditDateTime, EventID lastEventID)
        : this(accountID, name, formalName, bookCurrency, bookCostBasis, identifiers, active, displayOrder, valuationDateTime, auditDateTime, lastEventID, new LastAuditDateTime(auditDateTime.Value))
    {
    }

    public override string ToString() => Name;
}
