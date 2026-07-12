using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Broker Trade Method Unset Event")]
public sealed record BrokerTradeMethodUnsetEvent : EventBase, IBrokerEvent
{
    [EventProperty(Description = "LEI")]
    public LegalEntityIdentifier LEI { get; init; }
    [EventProperty(Description = "Trade Method Type")]
    public TradeMethodType TradeMethodType { get; init; }

    public BrokerTradeMethodUnsetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, TradeMethodType tradeMethodType)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        LEI = lei;
        TradeMethodType = tradeMethodType;
    }
    public override string Type => nameof(BrokerTradeMethodUnsetEvent);
}
