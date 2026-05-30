using System.Text.Json.Serialization;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[JsonDerivedType(typeof(UserValuationPreferencesCreatedEvent), nameof(UserValuationPreferencesCreatedEvent))]
[JsonDerivedType(typeof(UserValuationPreferencesModifiedEvent), nameof(UserValuationPreferencesModifiedEvent))]
public interface IUserValuationPreferencesEvent : IEventBase
{
}
