using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IHoldingCreatedRequest : IEventRequest
{
    HoldingID? HoldingID { get; }
    AccountID AccountID { get; }
    InstrumentID InstrumentID { get; }
    string Name { get; }
    bool Active { get; }
    bool Default { get; }
}

public interface IHoldingBankCreatedRequest : IHoldingCreatedRequest
{
    string BankName { get; }
    string AccountName { get; }
    SortCode SortCode { get; }
    BankAccountNumber AccountNumber { get; }
    BIC BIC { get; }
    IBAN IBAN { get; }
}

public sealed record HoldingPositionMemoCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;

public sealed record HoldingPositionCashCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;

public sealed record HoldingCashDebtCreatedRequest(
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
    IBAN IBAN) : IHoldingBankCreatedRequest;

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
    IBAN IBAN) : IHoldingBankCreatedRequest;

public sealed record HoldingCashNonInvestableCreatedRequest(
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
    IBAN IBAN) : IHoldingBankCreatedRequest;

public sealed record HoldingInflowCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;

public sealed record HoldingOutflowCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;

public sealed record HoldingInspecieInCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;

public sealed record HoldingInspecieOutCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;

public sealed record HoldingFeesCustodianCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;

public sealed record HoldingFeesAdministratorCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;

public sealed record HoldingFeesBankCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;

public sealed record HoldingIncomeCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;

public sealed record HoldingInterestCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID? HoldingID,
    AccountID AccountID,
    InstrumentID InstrumentID,
    string Name,
    bool Active,
    bool Default) : IHoldingCreatedRequest;
