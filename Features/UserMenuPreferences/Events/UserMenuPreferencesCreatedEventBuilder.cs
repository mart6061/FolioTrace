using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class UserMenuPreferencesCreatedEventBuilder
{
    public static Result<UserMenuPreferencesCreatedEvent> Create(UserMenuPreferencesRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.Items);
    }

    public static Result<UserMenuPreferencesCreatedEvent> CreateDefault(UserCreatedEvent userCreatedEvent)
    {
        if (userCreatedEvent is null)
            throw new ArgumentNullException(nameof(userCreatedEvent));

        return Create(
            userCreatedEvent.UserID,
            userCreatedEvent.EventDateTime,
            $"Create default menu preferences for {userCreatedEvent.DisplayName}",
            UserMenuPreferenceDefaults.CreateVisibleItems());
    }

    public static Result<UserMenuPreferencesCreatedEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, IReadOnlyList<UserMenuPreferenceItem> items)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.CreateGuid7();
        return CreateSeed(eventID, userID, eventDateTime, auditDateTime, reason, items);
    }

    public static Result<UserMenuPreferencesCreatedEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, IReadOnlyList<UserMenuPreferenceItem> items)
    {
        var validationErrors = UserMenuPreferencesEventValidation.Validate(eventID, userID, eventDateTime, auditDateTime, reason, items);

        return validationErrors.Count == 0
            ? Result<UserMenuPreferencesCreatedEvent>.Success(new UserMenuPreferencesCreatedEvent(eventID, userID, eventDateTime, auditDateTime, reason, items))
            : Result<UserMenuPreferencesCreatedEvent>.Invalid(validationErrors);
    }
}
