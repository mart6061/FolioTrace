using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
public abstract record TradeFileEventBase : EventBase, ITradeFileEvent
{
    [EventProperty(Description = "Trade File ID")]
    public TradeFileID TradeFileID { get; init; }
    protected TradeFileEventBase(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TradeFileID tradeFileID) : base(eventID, userID, eventDateTime, auditDateTime, reason) => TradeFileID = tradeFileID;
}
