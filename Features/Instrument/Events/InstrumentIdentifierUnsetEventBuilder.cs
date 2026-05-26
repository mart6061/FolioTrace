using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class InstrumentIdentifierUnsetEventBuilder
{
    public static Result<InstrumentIdentifierUnsetEvent> Create(InstrumentIdentifierUnsetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = InstrumentCreatedEventBuilder.ValidateCommon(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID);

        return validationErrors.Count == 0
            ? Result<InstrumentIdentifierUnsetEvent>.Success(new InstrumentIdentifierUnsetEvent(eventId, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.InstrumentID, request.IdentifierType))
            : Result<InstrumentIdentifierUnsetEvent>.Invalid(validationErrors);
    }
}
