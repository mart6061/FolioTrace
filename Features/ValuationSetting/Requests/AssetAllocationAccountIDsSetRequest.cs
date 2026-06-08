using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationAccountIDsSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    AssetAllocationID AssetAllocationID,
    List<AccountID> AccountIDs) : IEventRequest;
