using FolioTrace.Aggregates; using FolioTrace.Types;
namespace Services;
public sealed class EffectiveDateControlSettingsService(DateControlSettingsService globalService, UserDateControlSettingsService userService)
{
    public async Task<EffectiveDateControlSettings> Get(UserID userID, EventDateTime eventDateTime, AuditDateTime? auditDateTime = null) { var global = await globalService.Get(eventDateTime, auditDateTime); var user = await userService.Get(userID, eventDateTime, auditDateTime); return user.HasStoredConfiguration ? new(user.Configuration, DateControlSettingsSource.User, user.LastEventID, user.LastAuditDateTime) : new(global.Configuration, DateControlSettingsSource.Global, global.LastEventID, global.LastAuditDateTime); }
}
