using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record HoldingBankModifiedEvent : HoldingModifiedEvent
{
    public string BankName { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public SortCode SortCode { get; init; } = null!;
    public BankAccountNumber AccountNumber { get; init; } = null!;
    public BIC BIC { get; init; } = null!;
    public IBAN IBAN { get; init; } = null!;

    protected HoldingBankModifiedEvent() { }

    internal HoldingBankModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault)
    {
        BankName = bankName?.Trim() ?? string.Empty;
        AccountName = accountName?.Trim() ?? string.Empty;
        SortCode = sortCode ?? throw new ArgumentNullException(nameof(sortCode));
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        BIC = bic ?? throw new ArgumentNullException(nameof(bic));
        IBAN = iban ?? throw new ArgumentNullException(nameof(iban));
    }
}
