using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Transaction, Description = "Transaction Debit Event")]
public sealed record TransactionDebitEvent : EventBase, ITransactionMovementEvent
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
    [EventProperty(Description = "Local Cost")]
    public TransactionLocalCost LocalCost { get; init; } = null!;
    [EventProperty(Description = "Local Cost Currency")]
    public Alpha3 LocalCostCurrency { get; init; } = null!;
    [EventProperty(Description = "Book Cost")]
    public TransactionBookCost BookCost { get; init; } = null!;
    [EventProperty(Description = "Book Cost Source")]
    public BookCostSource BookCostSource { get; init; } = BookCostSource.SameCurrency;
    [EventProperty(Description = "Book Cost Estimated")]
    public bool BookCostEstimated { get; init; }
    [EventProperty(Description = "Settlement Date Time")]
    public SettlementDateTime SettlementDateTime { get; init; } = null!;

    [JsonConstructor]
    private TransactionDebitEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal TransactionDebitEvent(
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
        TransactionLocalCost localCost,
        Alpha3 localCostCurrency,
        TransactionBookCost bookCost,
        BookCostSource bookCostSource,
        bool bookCostEstimated)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        SettlementDateTime = settlementDateTime;
        EventSetID = eventSetID;
        EventIDGroup = eventIDGroup.ToList();
        HoldingID = holdingID;
        InstrumentID = instrumentID;
        AccountID = accountID;
        Quantity = quantity;
        LocalCost = localCost;
        LocalCostCurrency = localCostCurrency;
        BookCost = bookCost;
        BookCostSource = bookCostSource;
        BookCostEstimated = bookCostEstimated;
    }

    public override string Type => nameof(TransactionDebitEvent);
}
