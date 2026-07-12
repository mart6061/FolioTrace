using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Ticket Trade Execution Failed Event")]
public sealed record TicketTradeExecutionFailedEvent : TicketTradeExecutionEventBase
{
    [EventProperty(Description = "Error")]
    public string Error { get; init; }
    public TicketTradeExecutionFailedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, TradeMethodType tradeMethodType, LegalEntityIdentifier brokerLEI, TradeFileID? tradeFileID, string error)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber, tradeMethodType, brokerLEI, tradeFileID) => Error = error;
    public override string Type => nameof(TicketTradeExecutionFailedEvent);
}
