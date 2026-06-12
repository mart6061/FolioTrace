using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    Price TradedPrice,
    EventDateTime TradeDateTime,
    SettlementDateTime SettlementDateTime,
    IReadOnlyList<TicketTradeAllocation> Allocations);
