using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Created, Description = "Trade File Created Event")]
public sealed record TradeFileCreatedEvent : TradeFileEventBase
{
    [EventProperty(Description = "Stored File ID")] public StoredFileID StoredFileID { get; init; }
    [EventProperty(Description = "File Name")] public string FileName { get; init; }
    [EventProperty(Description = "Media Type")] public string MediaType { get; init; }
    [EventProperty(Description = "Byte Length")] public long ByteLength { get; init; }
    [EventProperty(Description = "SHA256")] public string SHA256 { get; init; }
    public TradeFileCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TradeFileID tradeFileID, StoredFileID storedFileID, string fileName, string mediaType, long byteLength, string sha256) : base(eventID, userID, eventDateTime, auditDateTime, reason, tradeFileID)
    { StoredFileID = storedFileID; FileName = fileName; MediaType = mediaType; ByteLength = byteLength; SHA256 = sha256; }
    public override string Type => nameof(TradeFileCreatedEvent);
}
