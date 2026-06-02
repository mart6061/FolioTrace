using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketProposalDecisionRequestedEvent> RequestProposalDecision(TicketApprovalRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (ticket is not null)
            {
                if (ticket.Stage != TicketStage.Proposal)
                    messages.Add("Ticket must be in Proposal stage.");
                if (ticket.ProposalDecision != TicketDecision.InProgress)
                    messages.Add("Proposal decision must be in progress.");
                if (ticket.ProposalTargetPrice is null)
                    messages.Add("Proposal target price is required before requesting a decision.");
                if (ticket.ProposalTotalAmount is null)
                    messages.Add("Proposal total quantity is required before requesting a decision.");
                if (ticket.ProposalAllocations.Count == 0)
                    messages.Add("Proposal allocations are required before requesting a decision.");
            }

            return messages.Count > 0
                ? Result<TicketProposalDecisionRequestedEvent>.Invalid(messages)
                : Result<TicketProposalDecisionRequestedEvent>.Success(new TicketProposalDecisionRequestedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));
        });
}
