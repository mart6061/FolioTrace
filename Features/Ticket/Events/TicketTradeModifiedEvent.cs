using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeModifiedEvent : TicketTradeEventBase
{
    [JsonConstructor]
    private TicketTradeModifiedEvent() : this(null!, null!, null!, null!, string.Empty, null!, null!, []) { }

    internal TicketTradeModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Price tradedPrice, IReadOnlyList<TicketTradeAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, tradedPrice, allocations) { }

    public override string Type => nameof(TicketTradeModifiedEvent);
}
