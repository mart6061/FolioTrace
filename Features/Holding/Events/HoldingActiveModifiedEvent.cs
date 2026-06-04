using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingActiveModifiedEvent : EventBase, IHoldingEvent
{
    public HoldingID HoldingID { get; init; } = null!;
    public Active Active { get; init; } = false;

    [JsonConstructor]
    private HoldingActiveModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }

    internal HoldingActiveModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, Active active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        HoldingID = holdingID;
        Active = active;
    }

    public override string Type => nameof(HoldingActiveModifiedEvent);
}
