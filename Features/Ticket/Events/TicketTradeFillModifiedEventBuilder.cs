using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketTradeFillModifiedEvent> ModifyFill(TicketTradeFillRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (ticket is not null && ticket.Stage != TicketStage.Trade)
                messages.Add("Fills can only be changed while the ticket is in trade.");
            if (request.BrokerLEI is null)
                messages.Add("BrokerLEI is required.");
            ValidatePrice(request.Price, "Price", messages);
            ValidatePositiveDecimal(request.Quantity, "Quantity", messages);
            ValidateTransactionBookCost(request.BookCost, "BookCost", messages);
            if (request.FillID is null || request.FillID == Guid.Empty)
                messages.Add("FillID is required.");
            if (ticket is not null && ticket.Fills.All(fill => fill.FillID != request.FillID))
                messages.Add($"FillID '{request.FillID}' is not on ticket '{request.TicketNumber}'.");

            return messages.Count > 0
                ? Result<TicketTradeFillModifiedEvent>.Invalid(messages)
                : Result<TicketTradeFillModifiedEvent>.Success(new TicketTradeFillModifiedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.FillID!.Value, request.BrokerLEI!, request.Price, request.Quantity, request.BookCost, request.Note ?? string.Empty));
        });
}
