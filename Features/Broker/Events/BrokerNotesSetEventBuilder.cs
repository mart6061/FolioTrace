using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class BrokerNotesSetEventBuilder
{
    public static Result<BrokerNotesSetEvent> Create(BrokerNotesSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.LEI, request.Notes);
    }

    public static Result<BrokerNotesSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, LegalEntityIdentifier lei, string notes)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = BrokerNotesSetEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, lei);

        return validationErrors.Count == 0
            ? Result<BrokerNotesSetEvent>.Success(new BrokerNotesSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, lei, notes))
            : Result<BrokerNotesSetEvent>.Invalid(validationErrors);
    }
}
