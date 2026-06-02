using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketAccountRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    AccountID AccountID);
