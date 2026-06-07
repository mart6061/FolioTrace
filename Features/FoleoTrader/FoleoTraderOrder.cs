using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FoleoTraderOrder(
    TicketNumber TicketNumber,
    string ClOrdID,
    FoleoTraderOrderStatus Status,
    decimal OrderQuantity,
    decimal FilledQuantity,
    Price Price,
    Alpha3 Currency,
    TicketSide Side,
    string SecurityID,
    string SecurityIDSource,
    string Symbol,
    string? LastExecID,
    string? LastError,
    DateTime SubmittedAt,
    DateTime UpdatedAt);
