using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TransactionRequest(
    HoldingID HoldingID,
    InstrumentID InstrumentID,
    AccountID AccountID,
    TransactionQuantity Quantity,
    TransactionLocalCost LocalCost,
    Alpha3 LocalCostCurrency,
    TransactionBookCost BookCost,
    BookCostSource BookCostSource,
    bool BookCostEstimated);
