using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketProposalAllocationSetEvent : TicketEventBase
{
    public string ProposalAllocation { get; init; } = string.Empty;

    [JsonConstructor]
    private TicketProposalAllocationSetEvent() : this(null!, null!, null!, null!, string.Empty, null!, string.Empty) { }

    internal TicketProposalAllocationSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, string proposalAllocation)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        ProposalAllocation = proposalAllocation ?? string.Empty;

    public override string Type => nameof(TicketProposalAllocationSetEvent);
}
