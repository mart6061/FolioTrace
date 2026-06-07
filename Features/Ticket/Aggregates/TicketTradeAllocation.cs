using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeAllocation(AccountID AccountID, decimal Quantity, decimal BookCost, HoldingID? CashHoldingID = null);
