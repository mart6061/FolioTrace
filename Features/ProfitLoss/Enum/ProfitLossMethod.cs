using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<ProfitLossMethod>))]
public enum ProfitLossMethod
{
    [Description("FIFO")]
    FIFO,

    [Description("LIFO")]
    LIFO,

    [Description("Weighted average")]
    RunningAverage
}
