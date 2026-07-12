using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Trade File Pending Event")]
public sealed record TicketTradeFilePendingEvent : TicketTradeExecutionEventBase
{
    public TicketTradeFilePendingEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, LegalEntityIdentifier brokerLEI)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, TradeMethodType.TradeFile, brokerLEI, null) { }
    public override string Type => nameof(TicketTradeFilePendingEvent);
}
