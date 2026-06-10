using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Trade Instruction Notes Set Event")]
public sealed record TicketTradeInstructionNotesSetEvent : TicketEventBase
{
    [EventProperty(Description = "Trade Instruction Notes")]
    public string TradeInstructionNotes { get; init; } = string.Empty;

    [JsonConstructor]
    private TicketTradeInstructionNotesSetEvent() : this(null!, null!, null!, null!, string.Empty, null!, string.Empty) { }

    internal TicketTradeInstructionNotesSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, string tradeInstructionNotes)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        TradeInstructionNotes = tradeInstructionNotes ?? string.Empty;

    public override string Type => nameof(TicketTradeInstructionNotesSetEvent);
}
