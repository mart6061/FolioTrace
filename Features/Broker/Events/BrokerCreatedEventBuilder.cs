using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class BrokerCreatedEventBuilder
{
    public static Result<BrokerCreatedEvent> Create(BrokerCreatedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.Name, request.LEI, request.Commission, request.Active, request.ApprovedDateTime, request.NextReview, request.Notes);
    }

    public static Result<BrokerCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, string name, LegalEntityIdentifier lei, FeeRate commission, Active active, EventDateTime approvedDateTime, EventDateTime nextReview, string notes)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = BrokerCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, name, lei, commission, approvedDateTime, nextReview);

        return validationErrors.Count == 0
            ? Result<BrokerCreatedEvent>.Success(new BrokerCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, name, lei, commission, active, approvedDateTime, nextReview, notes))
            : Result<BrokerCreatedEvent>.Invalid(validationErrors);
    }

    public static Result<BrokerCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, string name, LegalEntityIdentifier lei, FeeRate commission, Active active, EventDateTime approvedDateTime, EventDateTime nextReview, string notes)
    {
        var validationErrors = BrokerCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, name, lei, commission, approvedDateTime, nextReview);

        return validationErrors.Count == 0
            ? Result<BrokerCreatedEvent>.Success(new BrokerCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, name, lei, commission, active, approvedDateTime, nextReview, notes))
            : Result<BrokerCreatedEvent>.Invalid(validationErrors);
    }
}
