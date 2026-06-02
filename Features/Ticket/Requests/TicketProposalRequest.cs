using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketProposalRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    Price TargetPrice,
    TransactionQuantity TotalAmount,
    Alpha3? TradeCurrency,
    IReadOnlyList<TicketProposalAllocation> Allocations);
