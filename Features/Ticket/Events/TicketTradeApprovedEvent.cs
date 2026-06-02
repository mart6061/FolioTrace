using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeApprovedEvent : TicketEventBase
{
    [JsonConstructor]
    private TicketTradeApprovedEvent() : this(null!, null!, null!, null!, string.Empty, null!) { }

    internal TicketTradeApprovedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) { }

    public override string Type => nameof(TicketTradeApprovedEvent);
}
