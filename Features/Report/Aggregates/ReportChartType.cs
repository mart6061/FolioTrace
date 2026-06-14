using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(JsonStringEnumConverter<ReportChartType>))]
public enum ReportChartType
{
    Pie,
    Bar
}
