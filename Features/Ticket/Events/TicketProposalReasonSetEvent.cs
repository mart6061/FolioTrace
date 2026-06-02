using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketProposalReasonSetEvent : TicketEventBase
{
    public string ProposalReason { get; init; } = string.Empty;

    [JsonConstructor]
    private TicketProposalReasonSetEvent() : this(null!, null!, null!, null!, string.Empty, null!, string.Empty) { }

    internal TicketProposalReasonSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, string proposalReason)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) =>
        ProposalReason = proposalReason ?? string.Empty;

    public override string Type => nameof(TicketProposalReasonSetEvent);
}
