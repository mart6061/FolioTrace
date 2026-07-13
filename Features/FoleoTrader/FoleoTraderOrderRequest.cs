using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FoleoTraderOrderRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    TicketNumber TicketNumber,
    LegalEntityIdentifier BrokerLEI);
