using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketAccountAddedEvent> AddAccount(TicketAccountRequest request, Tickets tickets, Accounts accounts) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            ValidateAccount(request.AccountID, accounts, messages);
            if (ticket is not null && ticket.AccountIDs.Contains(request.AccountID))
                messages.Add($"AccountID '{request.AccountID}' is already on ticket '{request.TicketNumber}'.");

            return messages.Count > 0
                ? Result<TicketAccountAddedEvent>.Invalid(messages)
                : Result<TicketAccountAddedEvent>.Success(new TicketAccountAddedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.AccountID));
        });
}
