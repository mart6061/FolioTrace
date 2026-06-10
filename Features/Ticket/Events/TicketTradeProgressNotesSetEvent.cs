using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Trade Progress Notes Set Event")]
public sealed record TicketTradeProgressNotesSetEvent : TicketEventBase
{
    [EventProperty(Description = "Trade Progress Notes")]
    public string TradeProgressNotes { get; init; } = string.Empty;

    [JsonConstructor]
    private TicketTradeProgressNotesSetEvent() : this(null!, null!, null!, null!, string.Empty, null!, string.Empty) { }

    internal TicketTradeProgressNotesSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, string tradeProgressNotes)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        TradeProgressNotes = tradeProgressNotes ?? string.Empty;

    public override string Type => nameof(TicketTradeProgressNotesSetEvent);
}
