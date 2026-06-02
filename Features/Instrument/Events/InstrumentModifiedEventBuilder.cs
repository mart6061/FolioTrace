using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class InstrumentModifiedEventBuilder
{
    public static Result<InstrumentModifiedEvent> Create(InstrumentModifiedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID);
        InstrumentCreatedEventBuilder.ValidateDefinition(validationErrors, request.Name, request.FormalName, request.Exchange, request.CFI, request.IncomeCountry, request.PriceCountry, request.PriceCurrency);

        return validationErrors.Count == 0
            ? Result<InstrumentModifiedEvent>.Success(new InstrumentModifiedEvent(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.Name, request.FormalName, request.Exchange, request.CFI, request.Logo, request.IncomeCountry, request.PriceCountry, request.PriceCurrency))
            : Result<InstrumentModifiedEvent>.Invalid(validationErrors);
    }
}
