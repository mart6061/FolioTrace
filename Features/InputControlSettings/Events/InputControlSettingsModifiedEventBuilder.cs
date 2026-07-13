using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class InputControlSettingsModifiedEventBuilder
{
    public static Result<InputControlSettingsModifiedEvent> Create(InputControlSettingsRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.Settings);
    }

    public static Result<InputControlSettingsModifiedEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, IReadOnlyList<InputControlSettingDefinition> settings)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.CreateGuid7();
        return CreateSeed(eventID, userID, eventDateTime, auditDateTime, reason, settings);
    }

    public static Result<InputControlSettingsModifiedEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, IReadOnlyList<InputControlSettingDefinition> settings)
    {
        var validationErrors = InputControlSettingsEventValidation.Validate(eventID, userID, eventDateTime, auditDateTime, reason, settings);

        return validationErrors.Count == 0
            ? Result<InputControlSettingsModifiedEvent>.Success(new InputControlSettingsModifiedEvent(eventID, userID, eventDateTime, auditDateTime, reason, settings))
            : Result<InputControlSettingsModifiedEvent>.Invalid(validationErrors);
    }
}
