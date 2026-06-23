using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TransactionBookCostAdjustmentRequest(
    UserID UserID,
    string Reason,
    EventSetID EventSetID,
    TransactionBookCost BookCost,
    BookCostSource BookCostSource = BookCostSource.Correction,
    bool BookCostEstimated = false);
