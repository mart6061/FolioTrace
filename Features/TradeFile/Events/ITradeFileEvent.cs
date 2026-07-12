using FolioTrace.Common;
using FolioTrace.Types;
using System.Text.Json.Serialization;
namespace FolioTrace.Aggregates;
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TradeFileRequestedEvent), nameof(TradeFileRequestedEvent))]
[JsonDerivedType(typeof(TradeFileCreatedEvent), nameof(TradeFileCreatedEvent))]
[JsonDerivedType(typeof(TradeFileSentEvent), nameof(TradeFileSentEvent))]
[JsonDerivedType(typeof(TradeFileAcknowledgedEvent), nameof(TradeFileAcknowledgedEvent))]
[JsonDerivedType(typeof(TradeFileTicketConfirmedEvent), nameof(TradeFileTicketConfirmedEvent))]
[JsonDerivedType(typeof(TradeFileCompletedEvent), nameof(TradeFileCompletedEvent))]
[JsonDerivedType(typeof(TradeFileFailedEvent), nameof(TradeFileFailedEvent))]
public interface ITradeFileEvent : IEventBase { TradeFileID TradeFileID { get; } }
