namespace FolioTrace.Aggregates;
public sealed record TradeFileDeliveryRequest(
    Guid TradeFileID,
    string BrokerLEI,
    string FileName,
    string MediaType,
    long ContentLength,
    Stream Content,
    string AcknowledgementUrl,
    string ConfirmationUrl,
    List<TradeFileDeliveryTicket> Tickets,
    string? CallbackSecret = null) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
