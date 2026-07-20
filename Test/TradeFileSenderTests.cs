using System.Net;
using API.TradeFiles;
using FolioTrace.Aggregates;
using Microsoft.Extensions.Options;

namespace Test;

public sealed class TradeFileSenderTests
{
    [Fact]
    public async Task SendAsync_StreamsMultipartContentAndDisposesThePayload()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        var sender = new FoleoTraderTradeFileSender(httpClient, Options.Create(new TradeFileOptions { FoleoTraderReceiverUrl = "https://receiver/trade-files" }));
        var content = new TrackingStream("trade-data"u8.ToArray());
        var request = new TradeFileDeliveryRequest(
            Guid.CreateGuid7(),
            "5493001KJTIIGC8Y1R12",
            "trades.xlsx",
            "application/test",
            content.Length,
            content,
            "https://api/ack",
            "https://api/confirm",
            [new TradeFileDeliveryTicket(1, 2m, 3m)]);

        await sender.SendAsync(request, CancellationToken.None);

        Assert.True(content.WasDisposed);
        Assert.Contains("trade-data", handler.Body, StringComparison.Ordinal);
        Assert.Contains("trades.xlsx", handler.Body, StringComparison.Ordinal);
        Assert.Contains("5493001KJTIIGC8Y1R12", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_DisposesThePayloadWhenCancelled()
    {
        using var httpClient = new HttpClient(new CancelledHandler());
        var sender = new FoleoTraderTradeFileSender(httpClient, Options.Create(new TradeFileOptions { FoleoTraderReceiverUrl = "https://receiver/trade-files" }));
        var content = new TrackingStream("trade-data"u8.ToArray());
        var request = new TradeFileDeliveryRequest(
            Guid.CreateGuid7(),
            "5493001KJTIIGC8Y1R12",
            "trades.xlsx",
            "application/test",
            content.Length,
            content,
            "https://api/ack",
            "https://api/confirm",
            []);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SendAsync(request, new CancellationToken(canceled: true)));

        Assert.True(content.WasDisposed);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }

    private sealed class CancelledHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromCanceled<HttpResponseMessage>(cancellationToken);
    }

    private sealed class TrackingStream(byte[] content) : MemoryStream(content)
    {
        public bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
