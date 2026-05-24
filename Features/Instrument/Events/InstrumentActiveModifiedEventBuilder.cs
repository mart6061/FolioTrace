using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class InstrumentActiveModifiedEventBuilder
{
    public static Result<InstrumentActiveModifiedEvent> Create(InstrumentActiveModifiedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID);

        return validationErrors.Count == 0
            ? Result<InstrumentActiveModifiedEvent>.Success(new InstrumentActiveModifiedEvent(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.Active))
            : Result<InstrumentActiveModifiedEvent>.Invalid(validationErrors);
    }
}
