using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingCashInvestableModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default,
    string BankName,
    string AccountName,
    SortCode SortCode,
    BankAccountNumber AccountNumber,
    BIC BIC,
    IBAN IBAN) : IHoldingCashBaseModifiedRequest;
