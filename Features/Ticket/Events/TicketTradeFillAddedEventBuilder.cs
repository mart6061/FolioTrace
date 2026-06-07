using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketTradeFillAddedEvent> AddFill(TicketTradeFillRequest request, Tickets tickets) =>
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
            var fillID = request.FillID ?? Guid.CreateGuid7();
            if (fillID == Guid.Empty)
                messages.Add("FillID is required.");

            return messages.Count > 0
                ? Result<TicketTradeFillAddedEvent>.Invalid(messages)
                : Result<TicketTradeFillAddedEvent>.Success(new TicketTradeFillAddedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, fillID, request.BrokerLEI!, request.Price, request.Quantity, request.BookCost, request.Note ?? string.Empty));
        });
}
