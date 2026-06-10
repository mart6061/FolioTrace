using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Cancelled, Description = "Ticket Cancelled Event")]
public sealed record TicketCancelledEvent : TicketEventBase
{
    [JsonConstructor]
    private TicketCancelledEvent() : this(null!, null!, null!, null!, string.Empty, null!) { }

    internal TicketCancelledEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) { }

    public override string Type => nameof(TicketCancelledEvent);
}
