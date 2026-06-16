using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationCreatedRequest(
    UserID UserID,
    AssetAllocationID? AssetAllocationID,
    string Name,
    List<AccountID> AccountIDs,
    bool Active,
    NodeID? RootNodeID,
    List<AssetAllocationNode> Nodes) : IConfigEventRequest;
