using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    EventDateTime EffectiveDateTime,
    string Reason,
    AssetAllocationID AssetAllocationID,
    string Name,
    NodeID RootNodeID,
    List<AssetAllocationNode> Nodes) : IEventRequest;
