using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketProposalCreatedEvent : TicketProposalEventBase
{
    [JsonConstructor]
    private TicketProposalCreatedEvent() : this(null!, null!, null!, null!, string.Empty, null!, null!, null!, null!, []) { }

    internal TicketProposalCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Price targetPrice, TransactionQuantity totalAmount, Alpha3 tradeCurrency, IReadOnlyList<TicketProposalAllocation> allocations)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, targetPrice, totalAmount, tradeCurrency, allocations) { }

    public override string Type => nameof(TicketProposalCreatedEvent);
}
