using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportNodeCash(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title,
    AssetAllocationID AssetAllocationID) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title);
