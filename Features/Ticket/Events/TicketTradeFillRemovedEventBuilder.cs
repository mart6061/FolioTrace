using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketTradeFillRemovedEvent> RemoveFill(TicketTradeFillRemovedRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (request.FillID == Guid.Empty)
                messages.Add("FillID is required.");
            if (ticket is not null && ticket.Fills.All(fill => fill.FillID != request.FillID))
                messages.Add($"FillID '{request.FillID}' is not on ticket '{request.TicketNumber}'.");

            return messages.Count > 0
                ? Result<TicketTradeFillRemovedEvent>.Invalid(messages)
                : Result<TicketTradeFillRemovedEvent>.Success(new TicketTradeFillRemovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.FillID));
        });
}
