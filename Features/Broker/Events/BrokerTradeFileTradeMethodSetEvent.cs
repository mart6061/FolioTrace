using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Broker Trade File Trade Method Set Event")]
public sealed record BrokerTradeFileTradeMethodSetEvent : BrokerTradeMethodSetEventBase
{
    public BrokerTradeFileTradeMethodSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, ITradeMethod tradeMethod)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, lei, tradeMethod) { }
    public override string Type => nameof(BrokerTradeFileTradeMethodSetEvent);
}
