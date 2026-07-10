using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class BrokerNextReviewSetEventBuilder
{
    public static Result<BrokerNextReviewSetEvent> Create(BrokerNextReviewSetRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.LEI, request.NextReview);
    }

    public static Result<BrokerNextReviewSetEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, LegalEntityIdentifier lei, EventDateTime nextReview)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = BrokerNextReviewSetEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, lei, nextReview);

        return validationErrors.Count == 0
            ? Result<BrokerNextReviewSetEvent>.Success(new BrokerNextReviewSetEvent(eventId, userId, eventDateTime, auditDateTime, reason, lei, nextReview))
            : Result<BrokerNextReviewSetEvent>.Invalid(validationErrors);
    }
}
