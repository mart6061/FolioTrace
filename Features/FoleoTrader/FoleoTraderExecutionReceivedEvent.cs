using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FoleoTraderExecutionReceivedEvent : EventBase, IFoleoTraderOrderEvent
{
    public TicketNumber TicketNumber { get; init; } = null!;

    public string ClOrdID { get; init; } = string.Empty;

    public string ExecID { get; init; } = string.Empty;

    public Guid FillID { get; init; }

    public Price Price { get; init; } = null!;

    public decimal Quantity { get; init; }

    public TransactionBookCost BookCost { get; init; } = null!;

    public decimal CumQuantity { get; init; }

    public decimal LeavesQuantity { get; init; }

    public string OrdStatus { get; init; } = string.Empty;

    private FoleoTraderExecutionReceivedEvent()
        : this(null!, null!, null!, null!, string.Empty, null!, string.Empty, string.Empty, Guid.Empty, null!, 0m, null!, 0m, 0m, string.Empty)
    {
    }

    public FoleoTraderExecutionReceivedEvent(
        EventID eventID,
        UserID userID,
        EventDateTime eventDateTime,
        AuditDateTime auditDateTime,
        string reason,
        TicketNumber ticketNumber,
        string clOrdID,
        string execID,
        Guid fillID,
        Price price,
        decimal quantity,
        TransactionBookCost bookCost,
        decimal cumQuantity,
        decimal leavesQuantity,
        string ordStatus)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        TicketNumber = ticketNumber;
        ClOrdID = clOrdID;
        ExecID = execID;
        FillID = fillID;
        Price = price;
        Quantity = quantity;
        BookCost = bookCost;
        CumQuantity = cumQuantity;
        LeavesQuantity = leavesQuantity;
        OrdStatus = ordStatus;
    }

    public override string Type => nameof(FoleoTraderExecutionReceivedEvent);
}
