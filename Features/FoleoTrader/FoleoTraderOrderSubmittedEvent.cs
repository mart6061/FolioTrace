using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.System, Description = "Foleo Trader Order Submitted Event")]
public sealed record FoleoTraderOrderSubmittedEvent : EventBase, IFoleoTraderOrderEvent
{
    [EventProperty(Description = "Ticket Number")]
    public TicketNumber TicketNumber { get; init; } = null!;

    [EventProperty(Description = "Cl Ord ID")]
    public string ClOrdID { get; init; } = string.Empty;

    [EventProperty(Description = "Side")]
    public TicketSide Side { get; init; }

    [EventProperty(Description = "Order Quantity")]
    public decimal OrderQuantity { get; init; }

    [EventProperty(Description = "Price")]
    public Price Price { get; init; } = null!;

    [EventProperty(Description = "Currency")]
    public Alpha3 Currency { get; init; } = null!;

    [EventProperty(Description = "Security ID")]
    public string SecurityID { get; init; } = string.Empty;

    [EventProperty(Description = "Security ID Source")]
    public string SecurityIDSource { get; init; } = string.Empty;

    [EventProperty(Description = "Symbol")]
    public string Symbol { get; init; } = string.Empty;

    private FoleoTraderOrderSubmittedEvent()
        : this(null!, null!, null!, null!, string.Empty, null!, string.Empty, TicketSide.Buy, 0m, null!, null!, string.Empty, string.Empty, string.Empty)
    {
    }

    public FoleoTraderOrderSubmittedEvent(
        EventID eventID,
        UserID userID,
        EventDateTime eventDateTime,
        AuditDateTime auditDateTime,
        string reason,
        TicketNumber ticketNumber,
        string clOrdID,
        TicketSide side,
        decimal orderQuantity,
        Price price,
        Alpha3 currency,
        string securityID,
        string securityIDSource,
        string symbol)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        TicketNumber = ticketNumber;
        ClOrdID = clOrdID;
        Side = side;
        OrderQuantity = orderQuantity;
        Price = price;
        Currency = currency;
        SecurityID = securityID;
        SecurityIDSource = securityIDSource;
        Symbol = symbol;
    }

    public override string Type => nameof(FoleoTraderOrderSubmittedEvent);
}
