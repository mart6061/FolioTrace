using System.Text.Json.Serialization;
namespace FolioTrace.Aggregates;
[JsonConverter(typeof(JsonStringEnumConverter<TradeFileStatus>))]
public enum TradeFileStatus { Requested, Created, Sent, Acknowledged, InProgress, Completed, Failed }
