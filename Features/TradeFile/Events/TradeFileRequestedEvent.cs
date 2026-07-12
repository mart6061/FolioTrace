using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Trade File Requested Event")]
public sealed record TradeFileRequestedEvent : TradeFileEventBase
{
    [EventProperty(Description = "Broker LEI")] public LegalEntityIdentifier BrokerLEI { get; init; }
    [EventProperty(Description = "Broker Name")] public string BrokerName { get; init; }
    [EventProperty(Description = "File Name Template")] public FileNameTemplate FileNameTemplate { get; init; }
    [EventProperty(Description = "Columns")] public List<TradeFileColumn> Columns { get; init; }
    [EventProperty(Description = "Send Config")] public ITradeMethodFileSendConfig SendConfig { get; init; }
    [EventProperty(Description = "Tickets")] public List<TradeFileTicketSnapshot> Tickets { get; init; }
    public TradeFileRequestedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, TradeFileID tradeFileID, LegalEntityIdentifier brokerLEI, string brokerName, FileNameTemplate fileNameTemplate, List<TradeFileColumn> columns, ITradeMethodFileSendConfig sendConfig, List<TradeFileTicketSnapshot> tickets) : base(eventID, userID, eventDateTime, auditDateTime, reason, tradeFileID)
    { BrokerLEI = brokerLEI; BrokerName = brokerName; FileNameTemplate = fileNameTemplate; Columns = columns; SendConfig = sendConfig; Tickets = tickets; }
    public override string Type => nameof(TradeFileRequestedEvent);
}
