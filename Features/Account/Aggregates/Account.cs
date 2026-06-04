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
    public required Active Active { get; init; }
    public required DisplayOrder DisplayOrder { get; init; }
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Account(AccountID accountID, string name, string formalName, Alpha3 bookCurrency, Active active, DisplayOrder? displayOrder, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        AccountID = accountID;
        Name = name;
        FormalName = formalName;
        BookCurrency = bookCurrency;
        Active = active;
        DisplayOrder = displayOrder ?? new DisplayOrder(0);
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public Account(AccountID accountID, string name, string formalName, Alpha3 bookCurrency, Active active, DisplayOrder displayOrder, EventDateTime valuationDateTime, AuditDateTime auditDateTime, EventID lastEventID)
        : this(accountID, name, formalName, bookCurrency, active, displayOrder, valuationDateTime, auditDateTime, lastEventID, new LastAuditDateTime(auditDateTime.Value))
    {
    }

    public override string ToString() => Name;

    public string ToData() => $"{AccountID.ToData()}|{Name}|{FormalName}|{BookCurrency.ToData()}|{Active}|{DisplayOrder.ToData()}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Account)}: {Name} ({AccountID}, BookCurrency: {BookCurrency}, Active: {Active}, DisplayOrder: {DisplayOrder.ToDetail()}, ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastEventID: {LastEventID.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()})";
}
