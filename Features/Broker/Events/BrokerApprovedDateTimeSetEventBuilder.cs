using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class BrokerApprovedDateTimeSetEventBuilder
{
    public static Result<BrokerApprovedDateTimeSetEvent> Create(BrokerApprovedDateTimeSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.LEI, request.ApprovedDateTime);
    }

    public static Result<BrokerApprovedDateTimeSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, LegalEntityIdentifier lei, EventDateTime approvedDateTime)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = BrokerApprovedDateTimeSetEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, lei, approvedDateTime);

        return validationErrors.Count == 0
            ? Result<BrokerApprovedDateTimeSetEvent>.Success(new BrokerApprovedDateTimeSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, lei, approvedDateTime))
            : Result<BrokerApprovedDateTimeSetEvent>.Invalid(validationErrors);
    }
}
