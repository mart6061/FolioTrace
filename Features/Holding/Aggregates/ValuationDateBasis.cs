using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<ValuationDateBasis>))]
public enum ValuationDateBasis
{
    [Description("Execution")]
    EventDateTime,

    [Description("Settlement")]
    SettlementDateTime
}
