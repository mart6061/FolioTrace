using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketCancelledEvent> Cancel(TicketCancellationRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var _);

            return messages.Count > 0
                ? Result<TicketCancelledEvent>.Invalid(messages)
                : Result<TicketCancelledEvent>.Success(new TicketCancelledEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));
        });
}
