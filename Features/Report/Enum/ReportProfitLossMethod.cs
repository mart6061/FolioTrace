using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<ReportProfitLossMethod>))]
public enum ReportProfitLossMethod
{
    Default,
    FIFO,
    LIFO,
    RunningAverage
}
