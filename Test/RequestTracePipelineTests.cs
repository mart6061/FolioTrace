using System.Text;
using API;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Repository;

namespace Test;

public sealed class RequestTracePipelineTests
{
    [Fact]
    public void BoundedQueue_DropsOldestWithoutBlocking()
    {
        var queue = CreateQueue(2);
        var first = TraceEvent(Guid.CreateGuid7());
        var second = TraceEvent(Guid.CreateGuid7());
        var third = TraceEvent(Guid.CreateGuid7());

        Assert.True(queue.TryEnqueue(first));
        Assert.True(queue.TryEnqueue(second));
        Assert.True(queue.TryEnqueue(third));

        Assert.Equal(1, queue.DroppedEventCount);
        Assert.True(queue.TryDequeue(out var dequeuedSecond));
        Assert.True(queue.TryDequeue(out var dequeuedThird));
        Assert.Equal(second.RequestId, dequeuedSecond?.RequestId);
        Assert.Equal(third.RequestId, dequeuedThird?.RequestId);
    }

    [Fact]
    public async Task ForwardingCaptureStream_ForwardsCompleteResponseAndBoundsCapture()
    {
        await using var destination = new MemoryStream();
        await using var stream = new ForwardingCaptureStream(destination, 5);
        var payload = Encoding.UTF8.GetBytes("hello world");

        await stream.WriteAsync(payload);

        Assert.Equal(payload, destination.ToArray());
        Assert.Equal("hello", Encoding.UTF8.GetString(stream.CapturedBytes.Span));
        Assert.Equal(payload.Length, stream.TotalBytes);
        Assert.True(stream.IsTruncated);
    }

    [Fact]
    public async Task CaptureBodiesDisabled_WritesDirectlyAndQueuesRequestAndResponse()
    {
        var repository = new StubRequestTraceRepository();
        var services = new ServiceCollection()
            .AddSingleton<IRequestTraceRepository>(repository)
            .BuildServiceProvider();
        var settings = new RequestTraceSettingsService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RequestTraceOptions { CaptureBodies = false }),
            NullLogger<RequestTraceSettingsService>.Instance);
        var queue = CreateQueue(8);
        var context = new DefaultHttpContext();
        var destination = new MemoryStream();
        context.Request.PathBase = "/API";
        context.Request.Path = "/test";
        context.Response.Body = destination;
        var middleware = new RequestTraceCaptureMiddleware(
            async requestContext =>
            {
                Assert.Same(destination, requestContext.Response.Body);
                await requestContext.Response.WriteAsync("complete response");
            },
            NullLogger<RequestTraceCaptureMiddleware>.Instance,
            settings,
            queue);

        await middleware.InvokeAsync(context);

        Assert.Equal("complete response", Encoding.UTF8.GetString(destination.ToArray()));
        Assert.True(queue.TryDequeue(out var request));
        Assert.True(queue.TryDequeue(out var response));
        Assert.Equal(RequestTraceEventKinds.Request, request?.Kind);
        Assert.Equal(RequestTraceEventKinds.Response, response?.Kind);
        Assert.Null(response?.Message?.Body);
    }

    private static RequestTraceLogQueue CreateQueue(int capacity) =>
        new(Options.Create(new RequestTraceOptions { QueueCapacity = capacity }), NullLogger<RequestTraceLogQueue>.Instance);

    private static RequestTraceEvent TraceEvent(Guid requestId) => new() { RequestId = requestId };

    private sealed class StubRequestTraceRepository : IRequestTraceRepository
    {
        public Task AppendAsync(RequestTraceEvent traceEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<RequestTrace?> LoadAsync(Guid requestId, CancellationToken cancellationToken = default) => Task.FromResult<RequestTrace?>(null);
        public Task<RequestTraceSearchResult> SearchAsync(RequestTraceSearchCriteria criteria, CancellationToken cancellationToken = default) => Task.FromResult(new RequestTraceSearchResult([], 0, 1, 50));
        public Task<RequestTracePurgeResult> PurgeAsync(DateTime? beforeUtc, CancellationToken cancellationToken = default) => Task.FromResult(new RequestTracePurgeResult(0));
        public Task<RequestTraceSettings?> LoadSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult<RequestTraceSettings?>(null);
        public Task StoreSettingsAsync(RequestTraceSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
