using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FoleoTraderOrderSubmittedEvent : EventBase, IFoleoTraderOrderEvent
{
    public TicketNumber TicketNumber { get; init; } = null!;

    public string ClOrdID { get; init; } = string.Empty;

    public TicketSide Side { get; init; }

    public decimal OrderQuantity { get; init; }

    public Price Price { get; init; } = null!;

    public Alpha3 Currency { get; init; } = null!;

    public string SecurityID { get; init; } = string.Empty;

    public string SecurityIDSource { get; init; } = string.Empty;

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
