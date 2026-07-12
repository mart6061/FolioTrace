using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Trade File Failed Event")]
public sealed record TradeFileFailedEvent : TradeFileEventBase
{
    [EventProperty(Description = "Error")] public string Error { get; init; }
    public TradeFileFailedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TradeFileID tradeFileID, string error) : base(eventID, userID, eventDateTime, auditDateTime, reason, tradeFileID) => Error = error;
    public override string Type => nameof(TradeFileFailedEvent);
}
