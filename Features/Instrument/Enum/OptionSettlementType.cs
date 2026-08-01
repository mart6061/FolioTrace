using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<OptionSettlementType>))]
public enum OptionSettlementType
{
    Physical,
    Cash
}
