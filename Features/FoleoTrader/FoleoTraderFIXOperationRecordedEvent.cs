using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.System, Description = "Foleo Trader FIX Operation Recorded Event")]
public sealed record FoleoTraderFIXOperationRecordedEvent : EventBase, IFoleoTraderFIXOperationEvent
{
    [EventProperty(Description = "Direction")]
    public string Direction { get; init; } = string.Empty;

    [EventProperty(Description = "Channel")]
    public string Channel { get; init; } = string.Empty;

    [EventProperty(Description = "Session ID")]
    public string SessionID { get; init; } = string.Empty;

    [EventProperty(Description = "Msg Type")]
    public string MsgType { get; init; } = string.Empty;

    [EventProperty(Description = "Msg Seq Num")]
    public int? MsgSeqNum { get; init; }

    [EventProperty(Description = "Sender Comp ID")]
    public string SenderCompID { get; init; } = string.Empty;

    [EventProperty(Description = "Target Comp ID")]
    public string TargetCompID { get; init; } = string.Empty;

    [EventProperty(Description = "Sending Time")]
    public DateTime? SendingTime { get; init; }

    [EventProperty(Description = "Cl Ord ID")]
    public string ClOrdID { get; init; } = string.Empty;

    [EventProperty(Description = "Exec ID")]
    public string ExecID { get; init; } = string.Empty;

    [EventProperty(Description = "Raw Message")]
    public string RawMessage { get; init; } = string.Empty;

    private FoleoTraderFIXOperationRecordedEvent()
        : this(null!, null!, null!, null!, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, string.Empty, string.Empty, null, string.Empty, string.Empty, string.Empty)
    {
    }

    public FoleoTraderFIXOperationRecordedEvent(
        EventID eventID,
        UserID userID,
        EventDateTime eventDateTime,
        AuditDateTime auditDateTime,
        string reason,
        string direction,
        string channel,
        string sessionID,
        string msgType,
        int? msgSeqNum,
        string senderCompID,
        string targetCompID,
        DateTime? sendingTime,
        string clOrdID,
        string execID,
        string rawMessage)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        Direction = direction;
        Channel = channel;
        SessionID = sessionID;
        MsgType = msgType;
        MsgSeqNum = msgSeqNum;
        SenderCompID = senderCompID;
        TargetCompID = targetCompID;
        SendingTime = sendingTime;
        ClOrdID = clOrdID;
        ExecID = execID;
        RawMessage = rawMessage;
    }

    public override string Type => nameof(FoleoTraderFIXOperationRecordedEvent);
}

