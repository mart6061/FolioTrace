using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationActiveSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    AssetAllocationID AssetAllocationID,
    bool Active) : IEventRequest;
