using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class UserMenuPreferencesModifiedEventBuilder
{
    public static Result<UserMenuPreferencesModifiedEvent> Create(UserMenuPreferencesRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.Items);
    }

    public static Result<UserMenuPreferencesModifiedEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, IReadOnlyList<UserMenuPreferenceItem> items)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.CreateGuid7();
        return CreateSeed(eventID, userID, eventDateTime, auditDateTime, reason, items);
    }

    public static Result<UserMenuPreferencesModifiedEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, IReadOnlyList<UserMenuPreferenceItem> items)
    {
        var validationErrors = UserMenuPreferencesEventValidation.Validate(eventID, userID, eventDateTime, auditDateTime, reason, items);

        return validationErrors.Count == 0
            ? Result<UserMenuPreferencesModifiedEvent>.Success(new UserMenuPreferencesModifiedEvent(eventID, userID, eventDateTime, auditDateTime, reason, items))
            : Result<UserMenuPreferencesModifiedEvent>.Invalid(validationErrors);
    }
}
