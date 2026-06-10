using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Proposal Allocation Set Event")]
public sealed record TicketProposalAllocationSetEvent : TicketEventBase
{
    [EventProperty(Description = "Proposal Allocation")]
    public string ProposalAllocation { get; init; } = string.Empty;

    [JsonConstructor]
    private TicketProposalAllocationSetEvent() : this(null!, null!, null!, null!, string.Empty, null!, string.Empty) { }

    internal TicketProposalAllocationSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, string proposalAllocation)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        ProposalAllocation = proposalAllocation ?? string.Empty;

    public override string Type => nameof(TicketProposalAllocationSetEvent);
}
