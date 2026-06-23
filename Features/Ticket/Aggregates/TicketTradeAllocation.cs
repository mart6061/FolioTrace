using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeAllocation(
    AccountID AccountID,
    decimal Quantity,
    decimal SettlementAmount,
    HoldingID? CashHoldingID = null,
    decimal? BookCostOverride = null);
