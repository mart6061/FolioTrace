using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Proposal Reason Set Event")]
public sealed record TicketProposalReasonSetEvent : TicketEventBase
{
    [EventProperty(Description = "Proposal Reason")]
    public string ProposalReason { get; init; } = string.Empty;

    [JsonConstructor]
    private TicketProposalReasonSetEvent() : this(null!, null!, null!, null!, string.Empty, null!, string.Empty) { }

    internal TicketProposalReasonSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, string proposalReason)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        ProposalReason = proposalReason ?? string.Empty;

    public override string Type => nameof(TicketProposalReasonSetEvent);
}
