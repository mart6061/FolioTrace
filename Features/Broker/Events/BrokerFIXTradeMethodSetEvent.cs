using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Broker FIX Trade Method Set Event")]
public sealed record BrokerFIXTradeMethodSetEvent : BrokerTradeMethodSetEventBase
{
    public BrokerFIXTradeMethodSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, FIXTradeMethod tradeMethod)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, lei, tradeMethod) { }
    public override string Type => nameof(BrokerFIXTradeMethodSetEvent);
}
