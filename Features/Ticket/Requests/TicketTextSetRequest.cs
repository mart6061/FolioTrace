using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTextSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    string Value);
