using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeFillRemovedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    Guid FillID);
