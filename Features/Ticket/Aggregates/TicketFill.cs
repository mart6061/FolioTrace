using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketFill(
    Guid FillID,
    LegalEntityIdentifier BrokerLEI,
    Price Price,
    decimal Quantity,
    TransactionLocalCost SettlementAmount,
    string Note,
    TransactionBookCost? BookCostOverride = null);
