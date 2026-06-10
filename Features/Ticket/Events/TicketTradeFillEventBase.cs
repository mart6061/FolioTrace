using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record TicketTradeFillEventBase : TicketEventBase
{
    [EventProperty(Description = "Fill ID", Order = 100)]
    public Guid FillID { get; init; }

    [EventProperty(Description = "Broker LEI", Order = 110)]
    public LegalEntityIdentifier BrokerLEI { get; init; } = null!;

    [EventProperty(Description = "Price", Order = 120)]
    public Price Price { get; init; } = null!;

    [EventProperty(Description = "Quantity", Order = 130)]
    public decimal Quantity { get; init; }

    [EventProperty(Description = "Book Cost", Order = 140)]
    public TransactionBookCost BookCost { get; init; } = null!;

    [EventProperty(Description = "Note", Order = 150)]
    public string Note { get; init; } = string.Empty;

    protected TicketTradeFillEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TicketNumber ticketNumber, Guid fillID, LegalEntityIdentifier brokerLEI, Price price, decimal quantity, TransactionBookCost bookCost, string note)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, ticketNumber)
    {
        FillID = fillID;
        BrokerLEI = brokerLEI;
        Price = price;
        Quantity = quantity;
        BookCost = bookCost;
        Note = note ?? string.Empty;
    }
}
