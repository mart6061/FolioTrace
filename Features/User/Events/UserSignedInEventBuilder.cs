using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class UserSignedInEventBuilder
{
    public static Result<UserSignedInEvent> Create(UserID userId, EventDateTime eventDateTime, string reason)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.NewGuid();
        var validationErrors = UserSignedInEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason);

        return validationErrors.Count == 0
            ? Result<UserSignedInEvent>.Success(new UserSignedInEvent(eventId, userId, eventDateTime, auditDateTime, reason))
            : Result<UserSignedInEvent>.Invalid(validationErrors);
    }

    public static Result<UserSignedInEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason)
    {
        var validationErrors = UserSignedInEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason);

        return validationErrors.Count == 0
            ? Result<UserSignedInEvent>.Success(new UserSignedInEvent(eventId, userId, eventDateTime, auditDateTime, reason))
            : Result<UserSignedInEvent>.Invalid(validationErrors);
    }
}
