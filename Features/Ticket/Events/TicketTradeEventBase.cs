using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record TicketTradeEventBase : TicketEventBase
{
    public Price TradedPrice { get; init; } = null!;
    public IReadOnlyList<TicketTradeAllocation> Allocations { get; init; } = [];

    protected TicketTradeEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Price tradedPrice, IReadOnlyList<TicketTradeAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        TradedPrice = tradedPrice;
        Allocations = allocations.ToList();
    }
}
