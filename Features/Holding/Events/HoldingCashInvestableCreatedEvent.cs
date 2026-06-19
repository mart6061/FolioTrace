using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Holding Cash Investable Created Event")]
public sealed record HoldingCashInvestableCreatedEvent : HoldingCashBaseCreatedEvent
{
    [JsonConstructor]
    private HoldingCashInvestableCreatedEvent() { }

    internal HoldingCashInvestableCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban) { }

    public override string Type => nameof(HoldingCashInvestableCreatedEvent);
    internal override HoldingBase CreateHolding() => new HoldingCashInvestable(HoldingID, AccountID, InstrumentID, Name, Active, Default, EventDateTime, AuditDateTime, EventID, AuditDateTime, BankName, AccountName, SortCode, AccountNumber, BIC, IBAN);
}
