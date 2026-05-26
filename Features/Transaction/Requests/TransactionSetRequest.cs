using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TransactionSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    IReadOnlyList<TransactionRequest> Credits,
    IReadOnlyList<TransactionRequest> Debits) : IEventRequest;
