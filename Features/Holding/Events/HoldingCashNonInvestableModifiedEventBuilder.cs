using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingCashNonInvestableModifiedEventBuilder
{
    public static Result<HoldingCashNonInvestableModifiedEvent> Create(HoldingCashNonInvestableModifiedRequest request, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateBank<HoldingCashNonInvestableModifiedEvent, HoldingCashNonInvestable>(request, CreateEvent, holdings);

    public static Result<HoldingCashNonInvestableModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateBankSeed<HoldingCashNonInvestableModifiedEvent, HoldingCashNonInvestable>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban, CreateEvent, holdings);

    private static HoldingCashNonInvestableModifiedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban);
}
