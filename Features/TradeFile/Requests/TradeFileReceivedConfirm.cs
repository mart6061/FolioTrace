namespace FolioTrace.Aggregates;
public sealed record TradeFileReceivedConfirm(Guid ConfirmationID, Guid TradeFileID, string BrokerLEI, DateTime ReceivedAtUtc);
