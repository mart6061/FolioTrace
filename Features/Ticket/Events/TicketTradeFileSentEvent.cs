using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Trade File Sent Event")]
public sealed record TicketTradeFileSentEvent : TicketTradeExecutionEventBase
{
    public TicketTradeFileSentEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, LegalEntityIdentifier brokerLEI, TradeFileID tradeFileID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, TradeMethodType.TradeFile, brokerLEI, tradeFileID) { }
    public override string Type => nameof(TicketTradeFileSentEvent);
}
