using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketTradeModifiedEvent> ModifyTrade(TicketTradeRequest request, Tickets tickets, Holdings? holdings = null, Instruments? instruments = null, bool allowExecutionLocked = false) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket, allowExecutionLocked);
            ValidateTradeEntry(ticket, messages, "modified");
            ValidatePrice(request.TradedPrice, "TradedPrice", messages);
            ValidateTradeDates(request.TradeDateTime, request.SettlementDateTime, messages);
            ValidateTradeAllocations(request.Allocations, ticket, holdings, instruments, messages);

            return messages.Count > 0
                ? Result<TicketTradeModifiedEvent>.Invalid(messages)
                : Result<TicketTradeModifiedEvent>.Success(new TicketTradeModifiedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.TradedPrice, request.TradeDateTime, request.SettlementDateTime, request.Allocations));
        });
}
