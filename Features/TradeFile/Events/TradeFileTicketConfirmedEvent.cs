using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Trade File Ticket Confirmed Event")]
public sealed record TradeFileTicketConfirmedEvent : TradeFileEventBase
{
    [EventProperty(Description = "Confirmation ID")] public Guid ConfirmationID { get; init; }
    [EventProperty(Description = "Ticket Number")] public TicketNumber TicketNumber { get; init; }
    [EventProperty(Description = "Quantity")] public decimal Quantity { get; init; }
    [EventProperty(Description = "Price")] public Price Price { get; init; }
    public TradeFileTicketConfirmedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TradeFileID tradeFileID, Guid confirmationID, TicketNumber ticketNumber, decimal quantity, Price price) : base(eventID, userID, eventDateTime, auditDateTime, reason, tradeFileID)
    { ConfirmationID = confirmationID; TicketNumber = ticketNumber; Quantity = quantity; Price = price; }
    public override string Type => nameof(TradeFileTicketConfirmedEvent);
}
