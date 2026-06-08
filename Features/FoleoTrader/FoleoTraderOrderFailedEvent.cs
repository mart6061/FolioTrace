using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FoleoTraderOrderFailedEvent : EventBase, IFoleoTraderOrderEvent
{
    public TicketNumber TicketNumber { get; init; } = null!;

    public string ClOrdID { get; init; } = string.Empty;

    public string Error { get; init; } = string.Empty;

    private FoleoTraderOrderFailedEvent()
        : this(null!, null!, null!, null!, string.Empty, null!, string.Empty, string.Empty)
    {
    }

    public FoleoTraderOrderFailedEvent(
        EventID eventID,
        UserID userID,
        EventDateTime eventDateTime,
        AuditDateTime auditDateTime,
        string reason,
        TicketNumber ticketNumber,
        string clOrdID,
        string error)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        TicketNumber = ticketNumber;
        ClOrdID = clOrdID;
        Error = error;
    }

    public override string Type => nameof(FoleoTraderOrderFailedEvent);
}
