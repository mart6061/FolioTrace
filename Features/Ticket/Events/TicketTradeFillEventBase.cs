using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record TicketTradeFillEventBase : TicketEventBase
{
    public Guid FillID { get; init; }
    public decimal Price { get; init; }
    public decimal Quantity { get; init; }
    public string Note { get; init; } = string.Empty;

    protected TicketTradeFillEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Guid fillID, decimal price, decimal quantity, string note)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        FillID = fillID;
        Price = price;
        Quantity = quantity;
        Note = note ?? string.Empty;
    }
}
