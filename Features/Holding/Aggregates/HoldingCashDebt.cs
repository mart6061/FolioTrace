using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingCashDebt : HoldingCashBase, IHoldingPosition
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingCashDebt(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime, bankName, accountName, sortCode, accountNumber, bic, iban)
    {
    }
}
