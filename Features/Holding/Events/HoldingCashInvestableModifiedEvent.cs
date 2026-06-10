using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Holding Cash Investable Modified Event")]
public sealed record HoldingCashInvestableModifiedEvent : HoldingBankModifiedEvent
{
    [JsonConstructor]
    private HoldingCashInvestableModifiedEvent() { }

    internal HoldingCashInvestableModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban) { }

    public override string Type => nameof(HoldingCashInvestableModifiedEvent);
    internal override Holding Apply(Holding holding) =>
        holding is HoldingCashInvestable existing
            ? existing with { Name = Name, Default = Default, BankName = BankName, AccountName = AccountName, SortCode = SortCode, AccountNumber = AccountNumber, BIC = BIC, IBAN = IBAN, ValuationDateTime = EventDateTime, AsOfDateTime = AuditDateTime, LastEventID = EventID, LastAuditDateTime = AuditDateTime }
            : throw new InvalidOperationException($"HoldingID '{HoldingID}' is not a {this.GetHoldingKindName()} holding.");
}
