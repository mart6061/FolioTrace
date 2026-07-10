using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class InputControlSettingsCreatedEventBuilder
{
    public static Result<InputControlSettingsCreatedEvent> Create(InputControlSettingsRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(request.UserID, request.EventDateTime, request.Reason, request.Settings);
    }

    public static Result<InputControlSettingsCreatedEvent> Create(UserID userID, EventDateTime eventDateTime, string reason, IReadOnlyList<InputControlSettingDefinition> settings)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventID = Guid.CreateGuid7();
        return CreateSeed(eventID, userID, eventDateTime, auditDateTime, reason, settings);
    }

    public static Result<InputControlSettingsCreatedEvent> CreateSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, IReadOnlyList<InputControlSettingDefinition> settings)
    {
        var validationErrors = InputControlSettingsEventValidation.Validate(eventID, userID, eventDateTime, auditDateTime, reason, settings);

        return validationErrors.Count == 0
            ? Result<InputControlSettingsCreatedEvent>.Success(new InputControlSettingsCreatedEvent(eventID, userID, eventDateTime, auditDateTime, reason, settings))
            : Result<InputControlSettingsCreatedEvent>.Invalid(validationErrors);
    }
}
