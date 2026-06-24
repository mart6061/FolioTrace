using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportNodeProfitLoss(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title,
    AssetAllocationID AssetAllocationID,
    ReportProfitLossMethod ProfitLossMethod = ReportProfitLossMethod.Default) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title);
