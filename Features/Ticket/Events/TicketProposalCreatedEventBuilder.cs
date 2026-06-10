using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketProposalCreatedEvent> CreateProposal(TicketProposalRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            ValidateProposalEntry(ticket, messages, "created");
            ValidatePrice(request.TargetPrice, "TargetPrice", messages);
            ValidateProposalAllocations(request.Allocations, ticket, messages);
            var tradeCurrency = ResolveTradeCurrency(request, ticket, messages);

            return messages.Count > 0
                ? Result<TicketProposalCreatedEvent>.Invalid(messages)
                : Result<TicketProposalCreatedEvent>.Success(new TicketProposalCreatedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.TargetPrice, tradeCurrency!, request.Allocations));
        });
}
