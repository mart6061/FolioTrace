using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportNodeChart(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title,
    AssetAllocationID AssetAllocationID,
    ReportChartType ChartType) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title)
{
    public int PieLevel { get; init; } = 1;
}
