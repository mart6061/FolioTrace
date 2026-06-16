using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationModifiedRequest(
    UserID UserID,
    AssetAllocationID AssetAllocationID,
    string Name,
    NodeID RootNodeID,
    List<AssetAllocationNode> Nodes) : IConfigEventRequest;
