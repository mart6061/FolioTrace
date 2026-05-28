using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record HoldingBankCreatedEvent : HoldingCreatedEvent
{
    public string BankName { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public SortCode SortCode { get; init; } = null!;
    public BankAccountNumber AccountNumber { get; init; } = null!;
    public BIC BIC { get; init; } = null!;
    public IBAN IBAN { get; init; } = null!;

    protected HoldingBankCreatedEvent() { }

    internal HoldingBankCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault)
    {
        BankName = bankName?.Trim() ?? string.Empty;
        AccountName = accountName?.Trim() ?? string.Empty;
        SortCode = sortCode ?? throw new ArgumentNullException(nameof(sortCode));
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        BIC = bic ?? throw new ArgumentNullException(nameof(bic));
        IBAN = iban ?? throw new ArgumentNullException(nameof(iban));
    }
}
