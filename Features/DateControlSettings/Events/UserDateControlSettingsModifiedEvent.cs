using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "User Date Control Settings Modified Event")]
public sealed record UserDateControlSettingsModifiedEvent : EventBase, IUserDateControlSettingsEvent
{
    [EventProperty(Description = "Configuration")] public DateControlConfiguration Configuration { get; init; } = DateControlConfiguration.Empty;
    [JsonConstructor] private UserDateControlSettingsModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }
    internal UserDateControlSettingsModifiedEvent(EventID id, UserID user, EventDateTime date, AuditDateTime audit, string reason, DateControlConfiguration configuration) : base(id, user, date, audit, reason) => Configuration = configuration;
    public override string Type => nameof(UserDateControlSettingsModifiedEvent);
}
