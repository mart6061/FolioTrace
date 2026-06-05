using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeFillAddedEvent : TicketTradeFillEventBase
{
    [JsonConstructor]
    private TicketTradeFillAddedEvent() : this(null!, null!, null!, null!, string.Empty, null!, Guid.Empty, null!, 0, 0, string.Empty) { }

    internal TicketTradeFillAddedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Guid fillID, LegalEntityIdentifier brokerLEI, decimal price, decimal quantity, string note)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, fillID, brokerLEI, price, quantity, note) { }

    public override string Type => nameof(TicketTradeFillAddedEvent);
}
