using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketTradeModifiedEvent> ModifyTrade(TicketTradeRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            ValidateTradeEntry(ticket, messages, "modified");
            ValidatePrice(request.TradedPrice, "TradedPrice", messages);
            ValidateTradeAllocations(request.Allocations, ticket, messages);

            return messages.Count > 0
                ? Result<TicketTradeModifiedEvent>.Invalid(messages)
                : Result<TicketTradeModifiedEvent>.Success(new TicketTradeModifiedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.TradedPrice, request.Allocations));
        });
}
