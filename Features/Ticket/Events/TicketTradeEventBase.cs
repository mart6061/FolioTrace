using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

public abstract record TicketTradeEventBase : TicketEventBase
{
    [EventProperty(Description = "Traded Price")]
    public Price TradedPrice { get; init; } = null!;
    [EventProperty(Description = "Trade Date Time")]
    public EventDateTime TradeDateTime { get; init; } = null!;
    [EventProperty(Description = "Settlement Date Time")]
    public SettlementDateTime SettlementDateTime { get; init; } = null!;
    [EventProperty(Description = "Allocations")]
    public IReadOnlyList<TicketTradeAllocation> Allocations { get; init; } = [];

    protected TicketTradeEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Price tradedPrice, EventDateTime tradeDateTime, SettlementDateTime settlementDateTime, IReadOnlyList<TicketTradeAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        TradedPrice = tradedPrice;
        TradeDateTime = tradeDateTime;
        SettlementDateTime = settlementDateTime;
        Allocations = allocations.ToList();
    }
}
