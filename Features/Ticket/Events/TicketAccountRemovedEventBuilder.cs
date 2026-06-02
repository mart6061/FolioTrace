using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketAccountRemovedEvent> RemoveAccount(TicketAccountRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (request.AccountID is null)
                messages.Add("AccountID is required.");
            if (ticket is not null && request.AccountID is not null && !ticket.AccountIDs.Contains(request.AccountID))
                messages.Add($"AccountID '{request.AccountID}' is not on ticket '{request.TicketNumber}'.");

            return messages.Count > 0
                ? Result<TicketAccountRemovedEvent>.Invalid(messages)
                : Result<TicketAccountRemovedEvent>.Success(new TicketAccountRemovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.AccountID!));
        });
}
