using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<TicketTradeExecutionStatus>))]
public enum TicketTradeExecutionStatus
{
    Ready,
    FIXRequested,
    PendingTradeFile,
    TradeFileRequested,
    TradeFileCreated,
    TradeFileSent,
    TradeFileAcknowledged,
    InProgress,
    Failed,
    Completed
}
