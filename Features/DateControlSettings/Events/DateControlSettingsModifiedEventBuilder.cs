using FolioTrace.Common; using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[Builder]
public static class DateControlSettingsModifiedEventBuilder
{
    public static Result<DateControlSettingsModifiedEvent> Create(DateControlSettingsRequest request) { var id = new EventID(Guid.CreateGuid7()); var audit = AuditDateTimeBuilder.Create(); var errors = EventFieldValidation.CommonFieldMessages(id, request.UserID, request.EventDateTime, audit, request.Reason); errors.AddRange(DateControlSettingsValidation.Validate(request.Configuration, false)); return errors.Count == 0 ? Result<DateControlSettingsModifiedEvent>.Success(new(id, request.UserID, request.EventDateTime, audit, request.Reason, request.Configuration)) : Result<DateControlSettingsModifiedEvent>.Invalid(errors); }
}
