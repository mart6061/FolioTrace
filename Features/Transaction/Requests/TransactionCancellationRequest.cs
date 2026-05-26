using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TransactionCancellationRequest(
    UserID UserID,
    string Reason,
    EventSetID EventSetID);
