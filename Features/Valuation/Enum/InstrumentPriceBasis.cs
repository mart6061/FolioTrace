using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<InstrumentPriceBasis>))]
public enum InstrumentPriceBasis
{
    [Description("Bid price")]
    Bid,

    [Description("Ask price")]
    Ask,

    [Description("Mid price")]
    Mid,

    [Description("Net Asset Value")]
    NAV
}
