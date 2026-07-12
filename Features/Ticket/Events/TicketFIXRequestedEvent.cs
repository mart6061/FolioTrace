using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket FIX Requested Event")]
public sealed record TicketFIXRequestedEvent : TicketTradeExecutionEventBase
{
    public TicketFIXRequestedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, LegalEntityIdentifier brokerLEI)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, TradeMethodType.FIX, brokerLEI, null) { }
    public override string Type => nameof(TicketFIXRequestedEvent);
}
