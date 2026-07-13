using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Trade File Sent Event")]
public sealed record TradeFileSentEvent(EventID EventID, UserID UserID, EventDateTime EventDateTime, AuditDateTime AuditDateTime, string Reason, TradeFileID TradeFileID) : TradeFileEventBase(EventID, UserID, EventDateTime, AuditDateTime, Reason, TradeFileID) { public override string Type => nameof(TradeFileSentEvent); }
