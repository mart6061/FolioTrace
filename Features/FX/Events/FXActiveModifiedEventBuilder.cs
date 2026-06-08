using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class FXActiveModifiedEventBuilder
{
    public static Result<FXActiveModifiedEvent> Create(FXActiveModifiedRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.Pair, request.Active);
    }

    public static Result<FXActiveModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, CurrencyPair pair, Active active)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = FXActiveModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, pair);

        return validationErrors.Count == 0
            ? Result<FXActiveModifiedEvent>.Success(new FXActiveModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, pair, active))
            : Result<FXActiveModifiedEvent>.Invalid(validationErrors);
    }
}
