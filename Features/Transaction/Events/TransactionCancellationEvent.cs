using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TransactionCancellationEvent : EventBase, ITransactionEvent
{
    public EventSetID EventSetID { get; init; } = null!;
    public IReadOnlyList<EventID> EventIDGroup { get; init; } = [];
    public AccountID AccountID { get; init; } = null!;
    public EventID CancelledEventID { get; init; } = null!;
    public IReadOnlyList<EventID> CancelledIDGroup { get; init; } = [];
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
