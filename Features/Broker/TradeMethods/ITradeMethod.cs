using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(TradeMethodJsonConverter))]

public interface ITradeMethod
{
    TradeMethodType Type { get; }
}
