using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Input Control Settings Created Event")]
public sealed record InputControlSettingsCreatedEvent : EventBase, IInputControlSettingsEvent
{
    [EventProperty(Description = "Settings")]
    public IReadOnlyList<InputControlSettingDefinition> Settings { get; init; } = [];

    [JsonConstructor]
    private InputControlSettingsCreatedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal InputControlSettingsCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, IReadOnlyList<InputControlSettingDefinition> settings)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        Settings = settings;
    }

    public override string Type => nameof(InputControlSettingsCreatedEvent);
}
