using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Holding Cash Debt Modified Event")]
public sealed record HoldingCashDebtModifiedEvent : HoldingCashBaseModifiedEvent
{
    [JsonConstructor]
    private HoldingCashDebtModifiedEvent() { }

    internal HoldingCashDebtModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban) { }

    public override string Type => nameof(HoldingCashDebtModifiedEvent);
    internal override HoldingBase Apply(HoldingBase holding) =>
        holding is HoldingCashDebt existing
            ? existing with { Name = Name, Default = Default, BankName = BankName, AccountName = AccountName, SortCode = SortCode, AccountNumber = AccountNumber, BIC = BIC, IBAN = IBAN, ValuationDateTime = EventDateTime, AsOfDateTime = AuditDateTime, LastEventID = EventID, LastAuditDateTime = AuditDateTime }
            : throw new InvalidOperationException($"HoldingID '{HoldingID}' is not a {this.GetHoldingKindName()} holding.");
}
