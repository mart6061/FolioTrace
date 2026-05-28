using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TransactionCreditEvent), nameof(TransactionCreditEvent))]
[JsonDerivedType(typeof(TransactionDebitEvent), nameof(TransactionDebitEvent))]
[JsonDerivedType(typeof(TransactionCancellationEvent), nameof(TransactionCancellationEvent))]
public interface ITransactionEvent : IEventBase
{
    SettlementDateTime SettlementDateTime { get; }
}

public interface ITransactionMovementEvent : ITransactionEvent
{
    EventSetID EventSetID { get; }

    IReadOnlyList<EventID> EventIDGroup { get; }

    HoldingID HoldingID { get; }

    InstrumentID InstrumentID { get; }

    AccountID AccountID { get; }

    TransactionQuantity Quantity { get; }

    TransactionBookCost BookCost { get; }
}
