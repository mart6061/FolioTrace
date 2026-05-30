using FolioTrace.Common;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(UserMenuPreferencesCreatedEvent), nameof(UserMenuPreferencesCreatedEvent))]
[JsonDerivedType(typeof(UserMenuPreferencesModifiedEvent), nameof(UserMenuPreferencesModifiedEvent))]
public interface IUserMenuPreferencesEvent : IEventBase
{
    IReadOnlyList<UserMenuPreferenceItem> Items { get; }
}
