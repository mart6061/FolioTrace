using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class UserCreatedEventBuilder
{
    public static Result<UserCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, string displayName, UserDisplayPreferences displayPreferences, UserProfileValuationPreferences valuationPreferences)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var validationErrors = UserCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, displayName, displayPreferences, valuationPreferences);

        return validationErrors.Count == 0
            ? Result<UserCreatedEvent>.Success(new UserCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, displayName, displayPreferences, valuationPreferences))
            : Result<UserCreatedEvent>.Invalid(validationErrors);
    }

    public static Result<UserCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, string displayName, UserDisplayPreferences displayPreferences, UserProfileValuationPreferences valuationPreferences)
    {
        var validationErrors = UserCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, displayName, displayPreferences, valuationPreferences);

        return validationErrors.Count == 0
            ? Result<UserCreatedEvent>.Success(new UserCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, displayName, displayPreferences, valuationPreferences))
            : Result<UserCreatedEvent>.Invalid(validationErrors);
    }
}
