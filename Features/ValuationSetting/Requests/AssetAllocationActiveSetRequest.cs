using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationActiveSetRequest(
    UserID UserID,
    AssetAllocationID AssetAllocationID,
    bool Active) : IConfigEventRequest;
