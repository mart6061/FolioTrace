using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketProposalModifiedEvent> ModifyProposal(TicketProposalRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            ValidateProposalEntry(ticket, messages, "modified");
            ValidatePrice(request.TargetPrice, "TargetPrice", messages);
            ValidateTransactionQuantity(request.TotalAmount, "TotalAmount", messages);
            ValidateProposalAllocations(request.Allocations, ticket, messages);
            var tradeCurrency = ResolveTradeCurrency(request, ticket, messages);

            return messages.Count > 0
                ? Result<TicketProposalModifiedEvent>.Invalid(messages)
                : Result<TicketProposalModifiedEvent>.Success(new TicketProposalModifiedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.TargetPrice, request.TotalAmount, tradeCurrency!, request.Allocations));
        });
}
