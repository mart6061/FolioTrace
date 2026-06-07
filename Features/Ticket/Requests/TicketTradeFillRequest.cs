using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeFillRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    Guid? FillID,
    LegalEntityIdentifier BrokerLEI,
    Price Price,
    decimal Quantity,
    TransactionBookCost BookCost,
    string? Note);
