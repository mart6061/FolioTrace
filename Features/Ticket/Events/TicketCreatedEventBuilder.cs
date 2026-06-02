using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketCreatedEvent> Create(TicketCreatedRequest request, TicketNumber ticketNumber, Instruments instruments) =>
        CreateResult(() =>
        {
            var messages = ValidateBase(request.UserID, request.EventDateTime, request.Reason);
            if (request.InstrumentID is null)
                messages.Add("InstrumentID is required.");
            if (!Enum.IsDefined(request.Side))
                messages.Add("Side is required.");
            var instrument = instruments.Items.SingleOrDefault(instrument => instrument.InstrumentID == request.InstrumentID && instrument.Active);
            if (instrument is null)
                messages.Add($"InstrumentID '{request.InstrumentID}' does not exist or is inactive.");

            return messages.Count > 0
                ? Result<TicketCreatedEvent>.Invalid(messages)
                : Result<TicketCreatedEvent>.Success(new TicketCreatedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, ticketNumber, request.Side, request.InstrumentID!, instrument!.PriceCurrency));
        });
}
