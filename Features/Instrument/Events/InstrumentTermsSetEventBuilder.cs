using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class InstrumentTermsSetEventBuilder
{
    public static Result<InstrumentTermsSetEvent> Create(InstrumentTermsSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        return CreateSeed(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.Terms);
    }

    public static Result<InstrumentTermsSetEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, InstrumentID instrumentID, IInstrumentTerms terms)
    {
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID);
        if (terms is null) validationErrors.Add("Terms are required.");

        return validationErrors.Count == 0
            ? Result<InstrumentTermsSetEvent>.Success(new InstrumentTermsSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, instrumentID, terms!))
            : Result<InstrumentTermsSetEvent>.Invalid(validationErrors);
    }
}
