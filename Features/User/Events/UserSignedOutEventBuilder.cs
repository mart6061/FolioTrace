using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class UserSignedOutEventBuilder
{
    public static Result<UserSignedOutEvent> Create(UserID userId, EventDateTime eventDateTime, string reason)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = UserSignedOutEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason);

        return validationErrors.Count == 0
            ? Result<UserSignedOutEvent>.Success(new UserSignedOutEvent(eventId, userId, eventDateTime, auditDateTime, reason))
            : Result<UserSignedOutEvent>.Invalid(validationErrors);
    }

    public static Result<UserSignedOutEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason)
    {
        var validationErrors = UserSignedOutEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason);

        return validationErrors.Count == 0
            ? Result<UserSignedOutEvent>.Success(new UserSignedOutEvent(eventId, userId, eventDateTime, auditDateTime, reason))
            : Result<UserSignedOutEvent>.Invalid(validationErrors);
    }
}
