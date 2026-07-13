using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record BrokerTradeMethodSetRequest(UserID UserID, EventDateTime EventDateTime, string Reason, LegalEntityIdentifier LEI, ITradeMethod TradeMethod) : IEventRequest;
