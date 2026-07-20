namespace FolioTrace.Aggregates;
public sealed record TradeFileDeliveryRequest(Guid TradeFileID, string BrokerLEI, string FileName, string MediaType, byte[] Content, string AcknowledgementUrl, string ConfirmationUrl, List<TradeFileDeliveryTicket> Tickets, string? CallbackSecret = null);
