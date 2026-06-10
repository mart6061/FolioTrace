using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Proposal Decision Requested Event")]
public sealed record TicketProposalDecisionRequestedEvent : TicketEventBase
{
    [JsonConstructor]
    private TicketProposalDecisionRequestedEvent() : this(null!, null!, null!, null!, string.Empty, null!) { }

    internal TicketProposalDecisionRequestedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber) { }

    public override string Type => nameof(TicketProposalDecisionRequestedEvent);
}
