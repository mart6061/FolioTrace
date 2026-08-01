using System.Text.Json.Serialization;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DateControlSettingsCreatedEvent), nameof(DateControlSettingsCreatedEvent))]
[JsonDerivedType(typeof(DateControlSettingsModifiedEvent), nameof(DateControlSettingsModifiedEvent))]
public interface IDateControlSettingsEvent : IEventBase { DateControlConfiguration Configuration { get; } }
