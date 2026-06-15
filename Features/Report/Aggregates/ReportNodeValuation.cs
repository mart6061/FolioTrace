using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportNodeValuation(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title,
    AssetAllocationID AssetAllocationID,
    List<ReportValuationColumn>? Columns = null) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title);
