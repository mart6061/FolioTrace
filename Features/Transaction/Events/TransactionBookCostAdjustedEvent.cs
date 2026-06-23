using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Transaction Book Cost Adjusted Event")]
public sealed record TransactionBookCostAdjustedEvent : EventBase, ITransactionEvent
{
    [EventProperty(Description = "Event Set ID")]
    public EventSetID EventSetID { get; init; } = null!;
    [EventProperty(Description = "Adjusted ID Group")]
    public IReadOnlyList<EventID> AdjustedIDGroup { get; init; } = [];
    [EventProperty(Description = "Account ID")]
    public AccountID AccountID { get; init; } = null!;
    [EventProperty(Description = "Book Cost")]
    public TransactionBookCost BookCost { get; init; } = null!;
    [EventProperty(Description = "Book Cost Source")]
    public BookCostSource BookCostSource { get; init; } = BookCostSource.Correction;
    [EventProperty(Description = "Book Cost Estimated")]
    public bool BookCostEstimated { get; init; }
    [EventProperty(Description = "Settlement Date Time")]
    public SettlementDateTime SettlementDateTime { get; init; } = null!;

    [JsonConstructor]
    private TransactionBookCostAdjustedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal TransactionBookCostAdjustedEvent(
        EventID eventId,
        UserID userId,
        EventDateTime eventDateTime,
        SettlementDateTime settlementDateTime,
        AuditDateTime auditDateTime,
        string reason,
        EventSetID eventSetID,
        IReadOnlyList<EventID> adjustedIDGroup,
        AccountID accountID,
        TransactionBookCost bookCost,
        BookCostSource bookCostSource,
        bool bookCostEstimated)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        SettlementDateTime = settlementDateTime;
        EventSetID = eventSetID;
        AdjustedIDGroup = adjustedIDGroup.ToList();
        AccountID = accountID;
        BookCost = bookCost;
        BookCostSource = bookCostSource;
        BookCostEstimated = bookCostEstimated;
    }

    public override string Type => nameof(TransactionBookCostAdjustedEvent);
}
