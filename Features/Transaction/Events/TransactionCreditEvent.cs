using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TransactionCreditEvent : EventBase, ITransactionMovementEvent
{
    public EventSetID EventSetID { get; init; } = null!;
    public IReadOnlyList<EventID> EventIDGroup { get; init; } = [];
    public HoldingID HoldingID { get; init; } = null!;
    public InstrumentID InstrumentID { get; init; } = null!;
    public AccountID AccountID { get; init; } = null!;
    public TransactionQuantity Quantity { get; init; } = null!;
    public TransactionBookCost BookCost { get; init; } = null!;
    public SettlementDateTime SettlementDateTime { get; init; } = null!;

    [JsonConstructor]
    private TransactionCreditEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal TransactionCreditEvent(
        EventID eventId,
        UserID userId,
        EventDateTime eventDateTime,
        SettlementDateTime settlementDateTime,
        AuditDateTime auditDateTime,
        string reason,
        EventSetID eventSetID,
        IReadOnlyList<EventID> eventIDGroup,
        HoldingID holdingID,
        InstrumentID instrumentID,
        AccountID accountID,
        TransactionQuantity quantity,
        TransactionBookCost bookCost)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        SettlementDateTime = settlementDateTime;
        EventSetID = eventSetID;
        EventIDGroup = eventIDGroup.ToList();
        HoldingID = holdingID;
        InstrumentID = instrumentID;
        AccountID = accountID;
        Quantity = quantity;
        BookCost = bookCost;
    }

    public override string Type => nameof(TransactionCreditEvent);
}
