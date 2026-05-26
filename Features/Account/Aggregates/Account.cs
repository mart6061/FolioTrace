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
    public required bool Active { get; init; }
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Account(AccountID accountID, string name, string formalName, Alpha3 bookCurrency, bool active, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        AccountID = accountID;
        Name = name;
        FormalName = formalName;
        BookCurrency = bookCurrency;
        Active = active;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public Account(AccountID accountID, string name, string formalName, Alpha3 bookCurrency, bool active, EventDateTime valuationDateTime, AuditDateTime auditDateTime, EventID lastEventID)
        : this(accountID, name, formalName, bookCurrency, active, valuationDateTime, auditDateTime, lastEventID, new LastAuditDateTime(auditDateTime.Value))
    {
    }

    public override string ToString() => Name;

    public string ToData() => $"{AccountID.ToData()}|{Name}|{FormalName}|{BookCurrency.ToData()}|{Active}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Account)}: {Name} ({AccountID}, BookCurrency: {BookCurrency}, Active: {Active}, ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastEventID: {LastEventID.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()})";
}
