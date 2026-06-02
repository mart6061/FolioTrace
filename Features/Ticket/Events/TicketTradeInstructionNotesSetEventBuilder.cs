using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketTradeInstructionNotesSetEvent> SetTradeInstructionNotes(TicketTextSetRequest request, Tickets tickets) =>
        CreateTradeTextSetEvent(request, tickets, () => new TicketTradeInstructionNotesSetEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.Value ?? string.Empty));
}
