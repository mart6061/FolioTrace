using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Cancelled, Description = "Transaction Cancellation Event")]
public sealed record TransactionCancellationEvent : EventBase, ITransactionEvent
{
    [EventProperty(Description = "Event Set ID")]
    public EventSetID EventSetID { get; init; } = null!;
    [EventProperty(Description = "Event ID Group")]
    public IReadOnlyList<EventID> EventIDGroup { get; init; } = [];
    [EventProperty(Description = "Account ID")]
    public AccountID AccountID { get; init; } = null!;
    [EventProperty(Description = "Cancelled Event ID")]
    public EventID CancelledEventID { get; init; } = null!;
    [EventProperty(Description = "Cancelled ID Group")]
    public IReadOnlyList<EventID> CancelledIDGroup { get; init; } = [];
    [EventProperty(Description = "Settlement Date Time")]
    public SettlementDateTime SettlementDateTime { get; init; } = null!;

    [JsonConstructor]
    private TransactionCancellationEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal TransactionCancellationEvent(
        EventID eventId,
        UserID userId,
        EventDateTime eventDateTime,
        SettlementDateTime settlementDateTime,
        AuditDateTime auditDateTime,
        string reason,
        EventSetID eventSetID,
        IReadOnlyList<EventID> eventIDGroup,
        AccountID accountID,
        EventID cancelledEventID,
        IReadOnlyList<EventID> cancelledIDGroup)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        SettlementDateTime = settlementDateTime;
        EventSetID = eventSetID;
        EventIDGroup = eventIDGroup.ToList();
        AccountID = accountID;
        CancelledEventID = cancelledEventID;
        CancelledIDGroup = cancelledIDGroup.ToList();
    }

    public override string Type => nameof(TransactionCancellationEvent);
}
