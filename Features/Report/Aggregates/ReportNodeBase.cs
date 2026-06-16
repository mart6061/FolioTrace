using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(ReportNodeBaseJsonConverter))]
public abstract record ReportNodeBase(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title)
{
    public ReportNodePageOrientation PageOrientation { get; init; } = ReportNodePageOrientation.Portrait;
}
