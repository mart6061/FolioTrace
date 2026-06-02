using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketApprovalRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber);
