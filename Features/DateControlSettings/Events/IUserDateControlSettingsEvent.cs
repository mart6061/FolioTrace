using System.Text.Json.Serialization;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(UserDateControlSettingsCreatedEvent), nameof(UserDateControlSettingsCreatedEvent))]
[JsonDerivedType(typeof(UserDateControlSettingsModifiedEvent), nameof(UserDateControlSettingsModifiedEvent))]
[JsonDerivedType(typeof(UserDateControlSettingsClearedEvent), nameof(UserDateControlSettingsClearedEvent))]
public interface IUserDateControlSettingsEvent : IEventBase { DateControlConfiguration Configuration { get; } }
