using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    EventDateTime EffectiveDateTime,
    string Reason,
    AssetAllocationID? AssetAllocationID,
    string Name,
    List<AccountID> AccountIDs,
    bool Active,
    NodeID? RootNodeID,
    List<AssetAllocationNode> Nodes) : IEventRequest;
