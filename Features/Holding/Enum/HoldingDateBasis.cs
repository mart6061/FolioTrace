using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<HoldingDateBasis>))]
public enum HoldingDateBasis
{
    [Description("Execution date and time")]
    EventDateTime,

    [Description("Settlement date and time")]
    SettlementDateTime
}
