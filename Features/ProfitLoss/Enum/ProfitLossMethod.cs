using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<ProfitLossMethod>))]
public enum ProfitLossMethod
{
    FIFO,
    LIFO,
    RunningAverage
}
