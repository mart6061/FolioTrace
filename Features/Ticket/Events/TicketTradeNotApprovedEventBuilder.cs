using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketTradeNotApprovedEvent> NotApproveTrade(TicketApprovalRequest request, Tickets tickets) =>
        CreateTradeDecisionEvent(request, tickets, () => new TicketTradeNotApprovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));
}
