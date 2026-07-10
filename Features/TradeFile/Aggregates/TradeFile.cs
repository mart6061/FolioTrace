using FolioTrace.Types;
namespace FolioTrace.Aggregates;
public sealed record TradeFile
{
    public required TradeFileID TradeFileID { get; init; }
    public required LegalEntityIdentifier BrokerLEI { get; init; }
    public required string BrokerName { get; init; }
    public required TradeFileStatus Status { get; init; }
    public required FileNameTemplate FileNameTemplate { get; init; }
    public required List<TradeFileColumn> Columns { get; init; }
    public required ITradeMethodFileSendConfig SendConfig { get; init; }
    public required List<TradeFileTicketSnapshot> Tickets { get; init; }
    public StoredFileID? StoredFileID { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public required List<TicketNumber> ConfirmedTickets { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }
}
