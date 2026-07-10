using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Input Control Settings Modified Event")]
public sealed record InputControlSettingsModifiedEvent : EventBase, IInputControlSettingsEvent
{
    [EventProperty(Description = "Settings")]
    public IReadOnlyList<InputControlSettingDefinition> Settings { get; init; } = [];

    [JsonConstructor]
    private InputControlSettingsModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal InputControlSettingsModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, IReadOnlyList<InputControlSettingDefinition> settings)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        Settings = settings;
    }

    public override string Type => nameof(InputControlSettingsModifiedEvent);
}
