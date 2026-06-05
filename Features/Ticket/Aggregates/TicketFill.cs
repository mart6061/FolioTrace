using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketFill(Guid FillID, LegalEntityIdentifier BrokerLEI, decimal Price, decimal Quantity, string Note);
