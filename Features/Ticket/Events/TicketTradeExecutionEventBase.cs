using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record TicketTradeExecutionEventBase : TicketEventBase
{
    [EventProperty(Description = "Trade Method Type")]
    public TradeMethodType TradeMethodType { get; init; }
    [EventProperty(Description = "Broker LEI")]
    public LegalEntityIdentifier BrokerLEI { get; init; }
    [EventProperty(Description = "Trade File ID")]
    public TradeFileID? TradeFileID { get; init; }

    protected TicketTradeExecutionEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, TradeMethodType tradeMethodType, LegalEntityIdentifier brokerLEI, TradeFileID? tradeFileID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        TradeMethodType = tradeMethodType;
        BrokerLEI = brokerLEI;
        TradeFileID = tradeFileID;
    }
}
