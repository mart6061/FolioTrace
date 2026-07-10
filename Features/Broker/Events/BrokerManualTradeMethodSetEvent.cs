using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Broker Manual Trade Method Set Event")]
public sealed record BrokerManualTradeMethodSetEvent : BrokerTradeMethodSetEventBase
{
    public BrokerManualTradeMethodSetEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, ManualTradeMethod tradeMethod)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, lei, tradeMethod) { }
    public override string Type => nameof(BrokerManualTradeMethodSetEvent);
}
