using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Trade File Acknowledged Event")]
public sealed record TradeFileAcknowledgedEvent : TradeFileEventBase
{
    [EventProperty(Description = "Confirmation ID")]
    public Guid ConfirmationID { get; init; }

    public TradeFileAcknowledgedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TradeFileID tradeFileID, Guid confirmationID)
        : base(eventID, userID, eventDateTime, auditDateTime, reason, tradeFileID) => ConfirmationID = confirmationID;

    public override string Type => nameof(TradeFileAcknowledgedEvent);
}
