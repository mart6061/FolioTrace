using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Holding Cash Base Created Event")]
public abstract record HoldingCashBaseCreatedEvent : HoldingCreatedEvent
{
    [EventProperty(Description = "Bank Name")]
    public string BankName { get; init; } = string.Empty;
    [EventProperty(Description = "Account Name")]
    public string AccountName { get; init; } = string.Empty;
    [EventProperty(Description = "Sort Code")]
    public SortCode SortCode { get; init; } = null!;
    [EventProperty(Description = "Account Number")]
    public BankAccountNumber AccountNumber { get; init; } = null!;
    [EventProperty(Description = "BIC")]
    public BIC BIC { get; init; } = null!;
    [EventProperty(Description = "IBAN")]
    public IBAN IBAN { get; init; } = null!;

    protected HoldingCashBaseCreatedEvent() { }

    internal HoldingCashBaseCreatedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
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
