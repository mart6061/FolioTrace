using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingCashDebtModifiedEventBuilder
{
    public static Result<HoldingCashDebtModifiedEvent> Create(HoldingCashDebtModifiedRequest request, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateBank<HoldingCashDebtModifiedEvent, HoldingCashDebt>(request, CreateEvent, holdings);

    public static Result<HoldingCashDebtModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateBankSeed<HoldingCashDebtModifiedEvent, HoldingCashDebt>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban, CreateEvent, holdings);

    private static HoldingCashDebtModifiedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban);
}
