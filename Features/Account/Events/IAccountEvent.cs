using System.Text.Json.Serialization;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(AccountCreatedEvent), nameof(AccountCreatedEvent))]
[JsonDerivedType(typeof(AccountModifiedEvent), nameof(AccountModifiedEvent))]
[JsonDerivedType(typeof(AccountActiveSetEvent), nameof(AccountActiveSetEvent))]
[JsonDerivedType(typeof(AccountDisplayOrderSetEvent), nameof(AccountDisplayOrderSetEvent))]
[JsonDerivedType(typeof(AccountIdentifierSetEvent), nameof(AccountIdentifierSetEvent))]
[JsonDerivedType(typeof(AccountIdentifierUnsetEvent), nameof(AccountIdentifierUnsetEvent))]
public interface IAccountEvent : IEventBase;
