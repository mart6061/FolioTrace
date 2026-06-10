using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Transaction, Description = "Transaction Credit Event")]
public sealed record TransactionCreditEvent : EventBase, ITransactionMovementEvent
{
    [EventProperty(Description = "Event Set ID")]
    public EventSetID EventSetID { get; init; } = null!;
    [EventProperty(Description = "Event ID Group")]
    public IReadOnlyList<EventID> EventIDGroup { get; init; } = [];
    [EventProperty(Description = "Holding ID")]
    public HoldingID HoldingID { get; init; } = null!;
    [EventProperty(Description = "Instrument ID")]
    public InstrumentID InstrumentID { get; init; } = null!;
    [EventProperty(Description = "Account ID")]
    public AccountID AccountID { get; init; } = null!;
    [EventProperty(Description = "Quantity")]
    public TransactionQuantity Quantity { get; init; } = null!;
    [EventProperty(Description = "Book Cost")]
    public TransactionBookCost BookCost { get; init; } = null!;
    [EventProperty(Description = "Settlement Date Time")]
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
