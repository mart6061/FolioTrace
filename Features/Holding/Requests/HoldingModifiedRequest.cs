using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IHoldingModifiedRequest : IEventRequest
{
    HoldingID HoldingID { get; }
    string Name { get; }
    bool Default { get; }
}

public interface IHoldingBankModifiedRequest : IHoldingModifiedRequest
{
    string BankName { get; }
    string AccountName { get; }
    SortCode SortCode { get; }
    BankAccountNumber AccountNumber { get; }
    BIC BIC { get; }
    IBAN IBAN { get; }
}

public sealed record HoldingPositionMemoModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;

public sealed record HoldingPositionCashModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;

public sealed record HoldingCashDebtModifiedRequest(
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
    IBAN IBAN) : IHoldingBankModifiedRequest;

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
    IBAN IBAN) : IHoldingBankModifiedRequest;

public sealed record HoldingCashNonInvestableModifiedRequest(
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
    IBAN IBAN) : IHoldingBankModifiedRequest;

public sealed record HoldingInflowModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;

public sealed record HoldingOutflowModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;

public sealed record HoldingFeesCustodianModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;

public sealed record HoldingFeesAdministratorModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;

public sealed record HoldingFeesBankModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;

public sealed record HoldingIncomeModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;

public sealed record HoldingInterestModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    HoldingID HoldingID,
    string Name,
    bool Default) : IHoldingModifiedRequest;
