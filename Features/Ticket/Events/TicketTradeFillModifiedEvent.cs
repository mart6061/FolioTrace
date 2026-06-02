using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeFillModifiedEvent : TicketTradeFillEventBase
{
    [JsonConstructor]
    private TicketTradeFillModifiedEvent() : this(null!, null!, null!, null!, string.Empty, null!, Guid.Empty, 0, 0, string.Empty) { }

    internal TicketTradeFillModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Guid fillID, decimal price, decimal quantity, string note)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, fillID, price, quantity, note) { }

    public override string Type => nameof(TicketTradeFillModifiedEvent);
}
