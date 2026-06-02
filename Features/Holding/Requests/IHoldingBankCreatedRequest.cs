using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IHoldingBankCreatedRequest : IHoldingCreatedRequest
{
    string BankName { get; }
    string AccountName { get; }
    SortCode SortCode { get; }
    BankAccountNumber AccountNumber { get; }
    BIC BIC { get; }
    IBAN IBAN { get; }
}
