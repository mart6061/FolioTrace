using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class UserModifiedEventBuilder
{
    public static Result<UserModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, string displayName, UserDisplayPreferences displayPreferences, UserProfileValuationPreferences valuationPreferences)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = UserModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, displayName, displayPreferences, valuationPreferences);

        return validationErrors.Count == 0
            ? Result<UserModifiedEvent>.Success(new UserModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, displayName, displayPreferences, valuationPreferences))
            : Result<UserModifiedEvent>.Invalid(validationErrors);
    }

    public static Result<UserModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, string displayName, UserDisplayPreferences displayPreferences, UserProfileValuationPreferences valuationPreferences)
    {
        var validationErrors = UserModifiedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, displayName, displayPreferences, valuationPreferences);

        return validationErrors.Count == 0
            ? Result<UserModifiedEvent>.Success(new UserModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, displayName, displayPreferences, valuationPreferences))
            : Result<UserModifiedEvent>.Invalid(validationErrors);
    }
}
