using System.Text.Json.Serialization;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(AccountCreatedEvent), nameof(AccountCreatedEvent))]
[JsonDerivedType(typeof(AccountModifiedEvent), nameof(AccountModifiedEvent))]
[JsonDerivedType(typeof(AccountActiveModifiedEvent), nameof(AccountActiveModifiedEvent))]
[JsonDerivedType(typeof(AccountDisplayOrderSetEvent), nameof(AccountDisplayOrderSetEvent))]
public interface IAccountEvent : IEventBase;
