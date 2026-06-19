using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Holding Cash Debt Created Event")]
public sealed record HoldingCashDebtCreatedEvent : HoldingCashBaseCreatedEvent
{
    [JsonConstructor]
    private HoldingCashDebtCreatedEvent() { }

    internal HoldingCashDebtCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban) { }

    public override string Type => nameof(HoldingCashDebtCreatedEvent);
    internal override HoldingBase CreateHolding() => new HoldingCashDebt(HoldingID, AccountID, InstrumentID, Name, Active, Default, EventDateTime, AuditDateTime, EventID, AuditDateTime, BankName, AccountName, SortCode, AccountNumber, BIC, IBAN);
}
