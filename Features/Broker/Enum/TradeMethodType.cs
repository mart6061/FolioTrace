using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<TradeMethodType>))]
public enum TradeMethodType
{
    FIX,
    Phone,
    Fax,
    TradeFile,
    Manual
}
