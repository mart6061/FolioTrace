using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketProposalAllocationSetEvent> SetProposalAllocation(TicketTextSetRequest request, Tickets tickets) =>
        CreateProposalTextSetEvent(request, tickets, () => new TicketProposalAllocationSetEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.Value ?? string.Empty));
}
