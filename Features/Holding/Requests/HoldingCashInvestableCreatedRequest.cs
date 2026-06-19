using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingCashInvestableCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default,
    string BankName,
    string AccountName,
    SortCode SortCode,
    BankAccountNumber AccountNumber,
    BIC BIC,
    IBAN IBAN) : IHoldingCashBaseCreatedRequest;
