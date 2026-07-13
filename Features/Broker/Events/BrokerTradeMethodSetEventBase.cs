using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record BrokerTradeMethodSetEventBase : EventBase, IBrokerEvent
{
    [EventProperty(Description = "LEI")]
    public LegalEntityIdentifier LEI { get; init; }

    [EventProperty(Description = "Trade Method")]
    public ITradeMethod TradeMethod { get; init; }

    protected BrokerTradeMethodSetEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, ITradeMethod tradeMethod)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        LEI = lei;
        TradeMethod = tradeMethod;
    }
}
