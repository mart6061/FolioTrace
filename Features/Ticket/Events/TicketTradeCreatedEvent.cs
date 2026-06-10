using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Ticket Trade Created Event")]
public sealed record TicketTradeCreatedEvent : TicketTradeEventBase
{
    [JsonConstructor]
    private TicketTradeCreatedEvent() : this(null!, null!, null!, null!, string.Empty, null!, null!, []) { }

    internal TicketTradeCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Price tradedPrice, IReadOnlyList<TicketTradeAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, tradedPrice, allocations) { }

    public override string Type => nameof(TicketTradeCreatedEvent);
}
