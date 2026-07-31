using FolioTrace.Common; using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[Builder]
public static class UserDateControlSettingsClearedEventBuilder
{
    public static Result<UserDateControlSettingsClearedEvent> Create(UserDateControlSettingsClearRequest request) { var id = new EventID(Guid.CreateGuid7()); var audit = AuditDateTimeBuilder.Create(); var errors = EventFieldValidation.CommonFieldMessages(id, request.UserID, request.EventDateTime, audit, request.Reason); return errors.Count == 0 ? Result<UserDateControlSettingsClearedEvent>.Success(new(id, request.UserID, request.EventDateTime, audit, request.Reason)) : Result<UserDateControlSettingsClearedEvent>.Invalid(errors); }
}
