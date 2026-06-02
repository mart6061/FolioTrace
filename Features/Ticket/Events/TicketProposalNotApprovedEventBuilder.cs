using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketProposalNotApprovedEvent> NotApproveProposal(TicketApprovalRequest request, Tickets tickets) =>
        CreateProposalDecisionEvent(request, tickets, () => new TicketProposalNotApprovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));
}
