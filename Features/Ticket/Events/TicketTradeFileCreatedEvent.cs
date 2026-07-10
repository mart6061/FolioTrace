using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Created, Description = "Ticket Trade File Created Event")]
public sealed record TicketTradeFileCreatedEvent : TicketTradeExecutionEventBase
{
    public TicketTradeFileCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, LegalEntityIdentifier brokerLEI, TradeFileID tradeFileID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, TradeMethodType.TradeFile, brokerLEI, tradeFileID) { }
    public override string Type => nameof(TicketTradeFileCreatedEvent);
}
