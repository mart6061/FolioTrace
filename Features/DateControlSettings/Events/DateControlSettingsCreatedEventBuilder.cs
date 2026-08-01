using FolioTrace.Common; using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[Builder]
public static class DateControlSettingsCreatedEventBuilder
{
    public static Result<DateControlSettingsCreatedEvent> Create(DateControlSettingsRequest request) => CreateSeed(Guid.CreateGuid7(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.Configuration);
    public static Result<DateControlSettingsCreatedEvent> CreateSeed(EventID id, UserID user, EventDateTime date, AuditDateTime audit, string reason, DateControlConfiguration configuration) { var errors = EventFieldValidation.CommonFieldMessages(id, user, date, audit, reason); errors.AddRange(DateControlSettingsValidation.Validate(configuration, false)); return errors.Count == 0 ? Result<DateControlSettingsCreatedEvent>.Success(new(id, user, date, audit, reason, configuration)) : Result<DateControlSettingsCreatedEvent>.Invalid(errors); }
}
