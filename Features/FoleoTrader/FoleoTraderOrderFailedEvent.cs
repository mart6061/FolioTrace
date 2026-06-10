using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.System, Description = "Foleo Trader Order Failed Event")]
public sealed record FoleoTraderOrderFailedEvent : EventBase, IFoleoTraderOrderEvent
{
    [EventProperty(Description = "Ticket Number")]
    public TicketNumber TicketNumber { get; init; } = null!;

    [EventProperty(Description = "Cl Ord ID")]
    public string ClOrdID { get; init; } = string.Empty;

    [EventProperty(Description = "Error")]
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
