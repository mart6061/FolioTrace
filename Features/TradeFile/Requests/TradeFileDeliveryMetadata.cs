namespace FolioTrace.Aggregates;

public sealed record TradeFileDeliveryMetadata(
    Guid TradeFileID,
    string BrokerLEI,
    string FileName,
    string MediaType,
    long ContentLength,
    string AcknowledgementUrl,
    string ConfirmationUrl,
    List<TradeFileDeliveryTicket> Tickets,
    string? CallbackSecret = null);
