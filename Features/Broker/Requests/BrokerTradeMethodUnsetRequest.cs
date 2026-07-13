using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record BrokerTradeMethodUnsetRequest(UserID UserID, EventDateTime EventDateTime, string Reason, LegalEntityIdentifier LEI, TradeMethodType TradeMethodType) : IEventRequest;
