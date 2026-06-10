using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Holding Active Modified Event")]
public sealed record HoldingActiveModifiedEvent : EventBase, IHoldingEvent
{
    [EventProperty(Description = "Holding ID")]
    public HoldingID HoldingID { get; init; } = null!;
    [EventProperty(Description = "Active")]
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
