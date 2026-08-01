using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Date Control Settings Created Event")]
public sealed record DateControlSettingsCreatedEvent : EventBase, IDateControlSettingsEvent
{
    [EventProperty(Description = "Configuration")] public DateControlConfiguration Configuration { get; init; } = DateControlConfiguration.Empty;
    [JsonConstructor] private DateControlSettingsCreatedEvent() : base(null!, null!, null!, null!, string.Empty) { }
    internal DateControlSettingsCreatedEvent(EventID id, UserID user, EventDateTime date, AuditDateTime audit, string reason, DateControlConfiguration configuration) : base(id, user, date, audit, reason) => Configuration = configuration;
    public override string Type => nameof(DateControlSettingsCreatedEvent);
}
