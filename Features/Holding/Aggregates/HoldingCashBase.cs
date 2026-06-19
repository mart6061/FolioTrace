using System.Diagnostics.CodeAnalysis;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record HoldingCashBase : HoldingBase
{
    public required string BankName { get; init; }
    public required string AccountName { get; init; }
    public required SortCode SortCode { get; init; }
    public required BankAccountNumber AccountNumber { get; init; }
    public required BIC BIC { get; init; }
    public required IBAN IBAN { get; init; }

    [SetsRequiredMembers]
    protected HoldingCashBase(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
        BankName = bankName?.Trim() ?? string.Empty;
        AccountName = accountName?.Trim() ?? string.Empty;
        SortCode = sortCode ?? throw new ArgumentNullException(nameof(sortCode));
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        BIC = bic ?? throw new ArgumentNullException(nameof(bic));
        IBAN = iban ?? throw new ArgumentNullException(nameof(iban));
    }
}
