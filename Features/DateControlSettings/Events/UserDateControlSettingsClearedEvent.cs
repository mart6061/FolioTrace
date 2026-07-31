using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "User Date Control Settings Cleared Event")]
public sealed record UserDateControlSettingsClearedEvent : EventBase, IUserDateControlSettingsEvent
{
    [EventProperty(Description = "Configuration")] public DateControlConfiguration Configuration { get; init; } = DateControlConfiguration.Empty;
    [JsonConstructor] private UserDateControlSettingsClearedEvent() : base(null!, null!, null!, null!, string.Empty) { }
    internal UserDateControlSettingsClearedEvent(EventID id, UserID user, EventDateTime date, AuditDateTime audit, string reason) : base(id, user, date, audit, reason) { }
    public override string Type => nameof(UserDateControlSettingsClearedEvent);
}
