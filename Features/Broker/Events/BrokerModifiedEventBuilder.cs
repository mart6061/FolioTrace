using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class BrokerModifiedEventBuilder
{
    public static Result<BrokerModifiedEvent> Create(BrokerModifiedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.LEI, request.Name, request.Commission);
    }

    public static Result<BrokerModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, LegalEntityIdentifier lei, string name, FeeRate commission)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = BrokerModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, lei, name, commission);

        return validationErrors.Count == 0
            ? Result<BrokerModifiedEvent>.Success(new BrokerModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, lei, name, commission))
            : Result<BrokerModifiedEvent>.Invalid(validationErrors);
    }
}
