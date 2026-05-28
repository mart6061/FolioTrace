using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingCashNonInvestableCreatedEvent : HoldingBankCreatedEvent
{
    [JsonConstructor]
    private HoldingCashNonInvestableCreatedEvent() { }

    internal HoldingCashNonInvestableCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban) { }

    public override string Type => nameof(HoldingCashNonInvestableCreatedEvent);
    internal override Holding CreateHolding() => new HoldingCashNonInvestable(HoldingID, AccountID, InstrumentID, Name, Active, Default, EventDateTime, AuditDateTime, EventID, AuditDateTime, BankName, AccountName, SortCode, AccountNumber, BIC, IBAN);
}
