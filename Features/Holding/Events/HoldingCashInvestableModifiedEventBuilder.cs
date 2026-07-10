using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class HoldingCashInvestableModifiedEventBuilder
{
    public static Result<HoldingCashInvestableModifiedEvent> Create(HoldingCashInvestableModifiedRequest request, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateBank<HoldingCashInvestableModifiedEvent, HoldingCashInvestable>(request, CreateEvent, holdings);

    public static Result<HoldingCashInvestableModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateBankSeed<HoldingCashInvestableModifiedEvent, HoldingCashInvestable>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban, CreateEvent, holdings);

    private static HoldingCashInvestableModifiedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban);
}
