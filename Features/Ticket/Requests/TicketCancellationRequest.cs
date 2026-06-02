using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketCancellationRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber);
