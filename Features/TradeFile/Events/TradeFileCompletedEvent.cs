using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Trade File Completed Event")]
public sealed record TradeFileCompletedEvent(EventID EventID, UserID UserID, EventDateTime EventDateTime, AuditDateTime AuditDateTime, string Reason, TradeFileID TradeFileID) : TradeFileEventBase(EventID, UserID, EventDateTime, AuditDateTime, Reason, TradeFileID) { public override string Type => nameof(TradeFileCompletedEvent); }
