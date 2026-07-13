using FolioTrace.Common;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BrokerCreatedEvent), nameof(BrokerCreatedEvent))]
[JsonDerivedType(typeof(BrokerModifiedEvent), nameof(BrokerModifiedEvent))]
[JsonDerivedType(typeof(BrokerActiveSetEvent), nameof(BrokerActiveSetEvent))]
[JsonDerivedType(typeof(BrokerApprovedDateTimeSetEvent), nameof(BrokerApprovedDateTimeSetEvent))]
[JsonDerivedType(typeof(BrokerNextReviewSetEvent), nameof(BrokerNextReviewSetEvent))]
[JsonDerivedType(typeof(BrokerNotesSetEvent), nameof(BrokerNotesSetEvent))]
[JsonDerivedType(typeof(BrokerFIXTradeMethodSetEvent), nameof(BrokerFIXTradeMethodSetEvent))]
[JsonDerivedType(typeof(BrokerPhoneTradeMethodSetEvent), nameof(BrokerPhoneTradeMethodSetEvent))]
[JsonDerivedType(typeof(BrokerFaxTradeMethodSetEvent), nameof(BrokerFaxTradeMethodSetEvent))]
[JsonDerivedType(typeof(BrokerTradeFileTradeMethodSetEvent), nameof(BrokerTradeFileTradeMethodSetEvent))]
[JsonDerivedType(typeof(BrokerManualTradeMethodSetEvent), nameof(BrokerManualTradeMethodSetEvent))]
[JsonDerivedType(typeof(BrokerTradeMethodUnsetEvent), nameof(BrokerTradeMethodUnsetEvent))]
public interface IBrokerEvent : IEventBase
{
}
