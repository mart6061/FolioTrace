using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record TicketProposalEventBase : TicketEventBase
{
    public Price TargetPrice { get; init; } = null!;
    public TransactionQuantity TotalAmount { get; init; } = null!;
    public Alpha3 TradeCurrency { get; init; } = null!;
    public IReadOnlyList<TicketProposalAllocation> Allocations { get; init; } = [];

    protected TicketProposalEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Price targetPrice, TransactionQuantity totalAmount, Alpha3 tradeCurrency, IReadOnlyList<TicketProposalAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        TargetPrice = targetPrice;
        TotalAmount = totalAmount;
        TradeCurrency = tradeCurrency;
        Allocations = allocations.ToList();
    }
}
