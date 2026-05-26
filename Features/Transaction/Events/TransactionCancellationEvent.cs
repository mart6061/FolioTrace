using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TransactionCancellationEvent : EventBase, ITransactionEvent
{
    public EventSetID EventSetID { get; init; } = null!;
    public IReadOnlyList<EventID> EventIDGroup { get; init; } = [];
    public EventID CancelledEventID { get; init; } = null!;
    public IReadOnlyList<EventID> CancelledIDGroup { get; init; } = [];

    [JsonConstructor]
    private TransactionCancellationEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal TransactionCancellationEvent(
        EventID eventId,
        UserID userId,
        EventDateTime eventDateTime,
        AuditDateTime auditDateTime,
        string reason,
        EventSetID eventSetID,
        IReadOnlyList<EventID> eventIDGroup,
        EventID cancelledEventID,
        IReadOnlyList<EventID> cancelledIDGroup)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        EventSetID = eventSetID;
        EventIDGroup = eventIDGroup.ToList();
        CancelledEventID = cancelledEventID;
        CancelledIDGroup = cancelledIDGroup.ToList();
    }

    public override string Type => nameof(TransactionCancellationEvent);
}
