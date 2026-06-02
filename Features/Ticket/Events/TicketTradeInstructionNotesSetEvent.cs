using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeInstructionNotesSetEvent : TicketEventBase
{
    public string TradeInstructionNotes { get; init; } = string.Empty;

    [JsonConstructor]
    private TicketTradeInstructionNotesSetEvent() : this(null!, null!, null!, null!, string.Empty, null!, string.Empty) { }

    internal TicketTradeInstructionNotesSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, string tradeInstructionNotes)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        TradeInstructionNotes = tradeInstructionNotes ?? string.Empty;

    public override string Type => nameof(TicketTradeInstructionNotesSetEvent);
}
