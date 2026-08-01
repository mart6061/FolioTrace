using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Date Control Settings Modified Event")]
public sealed record DateControlSettingsModifiedEvent : EventBase, IDateControlSettingsEvent
{
    [EventProperty(Description = "Configuration")] public DateControlConfiguration Configuration { get; init; } = DateControlConfiguration.Empty;
    [JsonConstructor] private DateControlSettingsModifiedEvent() : base(null!, null!, null!, null!, string.Empty) { }
    internal DateControlSettingsModifiedEvent(EventID id, UserID user, EventDateTime date, AuditDateTime audit, string reason, DateControlConfiguration configuration) : base(id, user, date, audit, reason) => Configuration = configuration;
    public override string Type => nameof(DateControlSettingsModifiedEvent);
}
