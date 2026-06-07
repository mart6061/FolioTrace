using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<FoleoTraderOrderStatus>))]
public enum FoleoTraderOrderStatus
{
    Submitted,
    PartiallyFilled,
    Filled,
    Rejected,
    Failed
}
