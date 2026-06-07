using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketTradeCreatedEvent> CreateTrade(TicketTradeRequest request, Tickets tickets, Holdings? holdings = null, Instruments? instruments = null) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            ValidateTradeEntry(ticket, messages, "created");
            ValidatePrice(request.TradedPrice, "TradedPrice", messages);
            ValidateTradeAllocations(request.Allocations, ticket, holdings, instruments, messages);

            return messages.Count > 0
                ? Result<TicketTradeCreatedEvent>.Invalid(messages)
                : Result<TicketTradeCreatedEvent>.Success(new TicketTradeCreatedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.TradedPrice, request.Allocations));
        });
}
