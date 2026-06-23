using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface ITransactionMovementEvent : ITransactionEvent
{
    EventSetID EventSetID { get; }

    IReadOnlyList<EventID> EventIDGroup { get; }

    HoldingID HoldingID { get; }

    InstrumentID InstrumentID { get; }

    AccountID AccountID { get; }

    TransactionQuantity Quantity { get; }

    TransactionLocalCost LocalCost { get; }

    Alpha3 LocalCostCurrency { get; }

    TransactionBookCost BookCost { get; }

    BookCostSource BookCostSource { get; }

    bool BookCostEstimated { get; }
}
