using System.Net;
using FoleoTrader;
using FolioTrace.Aggregates;
using Microsoft.Extensions.Logging.Abstractions;

namespace Test;

public sealed class TradeFileConfirmationHostedServiceTests
{
    [Fact]
    public async Task CallbackTimeout_DoesNotStopTheHostedService()
    {
        var confirmationAttempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new CallbackHandler(confirmationAttempted);
        var httpClientFactory = new TestHttpClientFactory(handler);
        var timeProvider = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var simulator = new TradeFileSimulator(httpClientFactory, timeProvider);
        var metadata = new TradeFileDeliveryMetadata(
            Guid.NewGuid(),
            "broker",
            "trades.csv",
            "text/csv",
            1,
            "https://callbacks.test/acknowledgement",
            "https://callbacks.test/confirmation",
            [new TradeFileDeliveryTicket(1, 2m, 3m)],
            null);

        await simulator.ReceiveAsync(metadata, CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromSeconds(31));

        var service = new TradeFileConfirmationHostedService(
            simulator,
            NullLogger<TradeFileConfirmationHostedService>.Instance);
        await service.StartAsync(CancellationToken.None);

        await confirmationAttempted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(service.ExecuteTask);
        Assert.False(service.ExecuteTask.IsCompleted);
        Assert.Equal(0, simulator.Files.Single().ConfirmedTicketCount);

        await service.StopAsync(CancellationToken.None);
    }

    private sealed class CallbackHandler(TaskCompletionSource confirmationAttempted) : HttpMessageHandler
    {
        private int requestCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref requestCount) == 1)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

            confirmationAttempted.TrySetResult();
            throw new TaskCanceledException("The callback timed out.");
        }
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan duration) => utcNow += duration;
    }
}
