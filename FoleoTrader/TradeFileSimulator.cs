using System.Collections.Concurrent;
using System.Net.Http.Json;
using FolioTrace.Aggregates;

namespace FoleoTrader;

public sealed class TradeFileSimulator(IHttpClientFactory httpClientFactory, TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<Guid, ReceivedTradeFile> files = [];
    public IReadOnlyCollection<ReceivedTradeFile> Files => files.Values.ToList();

    public async Task ReceiveAsync(TradeFileDeliveryMetadata request, CancellationToken cancellationToken)
    {
        var receivedAt = timeProvider.GetUtcNow().UtcDateTime;
        var file = new ReceivedTradeFile(request, receivedAt, receivedAt.AddSeconds(30));
        if (!files.TryAdd(request.TradeFileID, file)) return;
        var acknowledgement = new TradeFileReceivedConfirm(Guid.NewGuid(), request.TradeFileID, request.BrokerLEI, receivedAt);
        using var response = await httpClientFactory.CreateClient().PostAsJsonAsync(request.AcknowledgementUrl, acknowledgement, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public IReadOnlyList<ReceivedTradeFile> Due() =>
        files.Values.Where(item => !item.IsComplete && item.NextConfirmationAtUtc <= timeProvider.GetUtcNow().UtcDateTime).ToList();

    public async Task ConfirmNextAsync(ReceivedTradeFile file, CancellationToken cancellationToken)
    {
        TradeFileDeliveryTicket ticket;
        lock (file)
        {
            ticket = file.Request.Tickets[file.ConfirmedTicketCount];
        }
        var factor = ticket.TicketNumber % 2 == 0 ? 1.01m : 0.99m;
        var confirmation = new TradeFileTradeConfirm(Guid.NewGuid(), file.Request.TradeFileID, ticket.TicketNumber, ticket.Quantity, decimal.Round(ticket.Price * factor, 8), timeProvider.GetUtcNow().UtcDateTime);
        using var response = await httpClientFactory.CreateClient().PostAsJsonAsync(file.Request.ConfirmationUrl, confirmation, cancellationToken);
        response.EnsureSuccessStatusCode();
        lock (file)
        {
            file.ConfirmedTicketCount++;
            file.NextConfirmationAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(30);
        }
    }
}

public sealed class ReceivedTradeFile(TradeFileDeliveryMetadata request, DateTime receivedAtUtc, DateTime nextConfirmationAtUtc)
{
    public TradeFileDeliveryMetadata Request { get; } = request;
    public DateTime ReceivedAtUtc { get; } = receivedAtUtc;
    public DateTime NextConfirmationAtUtc { get; set; } = nextConfirmationAtUtc;
    public int ConfirmedTicketCount { get; set; }
    public bool IsComplete => ConfirmedTicketCount >= Request.Tickets.Count;
}
