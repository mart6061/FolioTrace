using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationAccountIDsSetRequest(
    UserID UserID,
    AssetAllocationID AssetAllocationID,
    List<AccountID> AccountIDs) : IConfigEventRequest;
