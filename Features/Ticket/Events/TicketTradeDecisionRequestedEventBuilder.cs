using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketTradeDecisionRequestedEvent> RequestTradeDecision(TicketApprovalRequest request, Tickets tickets, Holdings? holdings = null, Instruments? instruments = null) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (ticket is not null)
            {
                if (ticket.Stage != TicketStage.Trade)
                    messages.Add("Ticket must be in Trade stage.");
                if (ticket.TradeDecision != TicketDecision.InProgress)
                    messages.Add("Trade decision must be in progress.");
                if (ticket.TradePrice is null)
                    messages.Add("Trade price is required before requesting a decision.");
                if (ticket.TradeAllocations.Count == 0)
                    messages.Add("Trade allocations are required before requesting a decision.");
                else
                    ValidateTradeAllocations(ticket.TradeAllocations, ticket, holdings, instruments, messages);
                if (ticket.Fills.Count == 0)
                    messages.Add("Fills are required before requesting a trade decision.");
            }

            return messages.Count > 0
                ? Result<TicketTradeDecisionRequestedEvent>.Invalid(messages)
                : Result<TicketTradeDecisionRequestedEvent>.Success(new TicketTradeDecisionRequestedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));
        });
}
