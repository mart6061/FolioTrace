using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

public abstract record TicketProposalEventBase : TicketEventBase
{
    [EventProperty(Description = "Target Price")]
    public Price TargetPrice { get; init; } = null!;
    [EventProperty(Description = "Trade Currency")]
    public Alpha3 TradeCurrency { get; init; } = null!;
    [EventProperty(Description = "Allocations")]
    public IReadOnlyList<TicketProposalAllocation> Allocations { get; init; } = [];

    protected TicketProposalEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Price targetPrice, Alpha3 tradeCurrency, IReadOnlyList<TicketProposalAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        TargetPrice = targetPrice;
        TradeCurrency = tradeCurrency;
        Allocations = allocations.ToList();
    }
}
