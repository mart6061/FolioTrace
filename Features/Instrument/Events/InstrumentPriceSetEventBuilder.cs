using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class InstrumentPriceSetEventBuilder
{
    public static Result<InstrumentPriceSetEvent> Create(InstrumentPriceSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        return CreateSeed(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.Price);
    }

    public static Result<InstrumentPriceSetEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentPrice price)
    {
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID);
        if (price is null) validationErrors.Add("Price is required.");

        return validationErrors.Count == 0
            ? Result<InstrumentPriceSetEvent>.Success(new InstrumentPriceSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID, price!))
            : Result<InstrumentPriceSetEvent>.Invalid(validationErrors);
    }
}
