using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeExecutionRequest(UserID UserID, EventDateTime EventDateTime, string Reason, TicketNumber TicketNumber, TradeMethodType TradeMethodType, LegalEntityIdentifier BrokerLEI, TradeFileID? TradeFileID = null) : IEventRequest;
