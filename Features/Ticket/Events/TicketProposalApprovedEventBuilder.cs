using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketProposalApprovedEvent> ApproveProposal(TicketApprovalRequest request, Tickets tickets) =>
        CreateProposalDecisionEvent(request, tickets, () => new TicketProposalApprovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));
}
