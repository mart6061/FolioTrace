using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeFillRemovedEvent : TicketEventBase
{
    public Guid FillID { get; init; }

    [JsonConstructor]
    private TicketTradeFillRemovedEvent() : this(null!, null!, null!, null!, string.Empty, null!, Guid.Empty) { }

    internal TicketTradeFillRemovedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Guid fillID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        FillID = fillID;

    public override string Type => nameof(TicketTradeFillRemovedEvent);
}
