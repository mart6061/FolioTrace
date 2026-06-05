using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class BrokerActiveSetEventBuilder
{
    public static Result<BrokerActiveSetEvent> Create(BrokerActiveSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.LEI, request.Active);
    }

    public static Result<BrokerActiveSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, LegalEntityIdentifier lei, Active active)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = BrokerActiveSetEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, lei);

        return validationErrors.Count == 0
            ? Result<BrokerActiveSetEvent>.Success(new BrokerActiveSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, lei, active))
            : Result<BrokerActiveSetEvent>.Invalid(validationErrors);
    }
}
