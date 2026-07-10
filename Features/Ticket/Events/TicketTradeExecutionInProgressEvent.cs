using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Trade Execution In Progress Event")]
public sealed record TicketTradeExecutionInProgressEvent : TicketTradeExecutionEventBase
{
    public TicketTradeExecutionInProgressEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, TradeMethodType tradeMethodType, LegalEntityIdentifier brokerLEI, TradeFileID? tradeFileID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, tradeMethodType, brokerLEI, tradeFileID) { }
    public override string Type => nameof(TicketTradeExecutionInProgressEvent);
}
