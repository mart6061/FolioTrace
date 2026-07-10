using System.Text.Json.Serialization;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[JsonDerivedType(typeof(InputControlSettingsCreatedEvent), nameof(InputControlSettingsCreatedEvent))]
[JsonDerivedType(typeof(InputControlSettingsModifiedEvent), nameof(InputControlSettingsModifiedEvent))]
public interface IInputControlSettingsEvent : IEventBase
{
}
