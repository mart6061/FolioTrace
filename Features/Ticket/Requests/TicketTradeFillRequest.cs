using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeFillRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    Guid? FillID,
    decimal Price,
    decimal Quantity,
    string? Note);
