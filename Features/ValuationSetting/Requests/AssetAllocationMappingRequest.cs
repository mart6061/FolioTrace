using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationMappingRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    AssetAllocationID AssetAllocationID,
    HoldingID HoldingID,
    NodeID NodeID) : IEventRequest;
