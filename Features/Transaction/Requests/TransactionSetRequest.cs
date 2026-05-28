using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TransactionSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    SettlementDateTime SettlementDateTime,
    string Reason,
    IReadOnlyList<TransactionRequest> Credits,
    IReadOnlyList<TransactionRequest> Debits) : IEventRequest;
