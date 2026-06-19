using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<InstrumentPriceBasis>))]
public enum InstrumentPriceBasis
{
    [Description("Bid")]
    Bid,

    [Description("Ask")]
    Ask,

    [Description("Mid")]
    Mid,

    [Description("NAV")]
    NAV
}
