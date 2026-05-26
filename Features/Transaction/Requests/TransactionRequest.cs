using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TransactionRequest(
    HoldingID HoldingID,
    InstrumentID InstrumentID,
    AccountID AccountID,
    TransactionQuantity Quantity,
    TransactionBookCost BookCost);
