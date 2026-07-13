using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Broker Phone Trade Method Set Event")]
public sealed record BrokerPhoneTradeMethodSetEvent : BrokerTradeMethodSetEventBase
{
    public BrokerPhoneTradeMethodSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, ITradeMethod tradeMethod)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, lei, tradeMethod) { }
    public override string Type => nameof(BrokerPhoneTradeMethodSetEvent);
}
