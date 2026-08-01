using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FoleoTraderOrder(
    TicketNumber TicketNumber,
    LegalEntityIdentifier BrokerLEI,
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
    OptionExecutionDetails? Option,
    string? LastExecID,
    string? LastError,
    DateTime SubmittedAt,
    DateTime UpdatedAt);
