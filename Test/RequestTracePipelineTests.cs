using System.Text;
using API;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Repository;
using System.Reflection;

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
        Assert.Equal(2, queue.BatchSize);
    }

    [Fact]
    public void RepositoryConfiguration_IncludesRequestTraceDuplicatedFieldIndexes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FolioTrace"] = "Host=localhost;Database=foliotrace_schema_test;Username=test;Password=test"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddFolioTraceRepository(configuration);
        using var provider = services.BuildServiceProvider();
        using var store = provider.GetRequiredService<IDocumentStore>();
        var dataSource = provider.GetRequiredService<NpgsqlDataSource>();

        var script = store.Storage.ToDatabaseScript();
        var connectionString = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);

        Assert.Contains("mt_doc_requesttraceevent", script, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(@"(?is)create index[^;]+recorded_at_utc", script);
        Assert.Matches(@"(?is)create index[^;]+request_id", script);
        Assert.DoesNotMatch(@"(?is)create index[^;]+source", script);
        Assert.Equal(900, connectionString.CommandTimeout);
    }

    [Fact]
    public void LegacyUiCleanup_DeletesOnlyUiTraceDocumentsRegardlessOfJsonCasing()
    {
        var field = typeof(MartenRequestTraceRepository).GetField(
            "PurgeLegacyUiEventsSql",
            BindingFlags.NonPublic | BindingFlags.Static);
        var sql = Assert.IsType<string>(field?.GetRawConstantValue());

        Assert.Contains("data ->> 'Source'", sql, StringComparison.Ordinal);
        Assert.Contains("data ->> 'source'", sql, StringComparison.Ordinal);
        Assert.Contains("= 'UI'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("= 'API'", sql, StringComparison.Ordinal);
        Assert.Null(typeof(RequestTraceEvent).GetProperty("Source"));
    }

    [Fact]
    public async Task StoredSettings_AlwaysRetainRequiredNoiseExclusions()
    {
        var repository = new StubRequestTraceRepository
        {
            StoredSettings = new RequestTraceSettings { ExcludedPathPrefixes = ["/custom"] }
        };
        var services = new ServiceCollection()
            .AddSingleton<IRequestTraceRepository>(repository)
            .BuildServiceProvider();
        var settingsService = new RequestTraceSettingsService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RequestTraceOptions()),
            NullLogger<RequestTraceSettingsService>.Instance);

        var settings = await settingsService.GetAsync();

        Assert.Contains("/custom", settings.ExcludedPathPrefixes);
        Assert.Contains("/Auth/Session", settings.ExcludedPathPrefixes);
        Assert.Contains("/Diagnostics/RequestTrace", settings.ExcludedPathPrefixes);
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
        var parentRequestId = Guid.CreateGuid7();
        context.Request.PathBase = "/API";
        context.Request.Path = "/test";
        context.Request.Headers[RequestTraceConstants.ParentRequestIdHeader] = parentRequestId.ToString();
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
        Assert.Equal(parentRequestId, request?.ParentRequestId);
        Assert.Equal(parentRequestId, response?.ParentRequestId);
        Assert.Null(response?.Message?.Body);
    }

    private static RequestTraceLogQueue CreateQueue(int capacity) =>
        new(Options.Create(new RequestTraceOptions { QueueCapacity = capacity }), NullLogger<RequestTraceLogQueue>.Instance);

    private static RequestTraceEvent TraceEvent(Guid requestId) => new() { RequestId = requestId };

    private sealed class StubRequestTraceRepository : IRequestTraceRepository
    {
        public RequestTraceSettings? StoredSettings { get; init; }
        public Task AppendAsync(RequestTraceEvent traceEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AppendAsync(IReadOnlyCollection<RequestTraceEvent> traceEvents, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<RequestTrace?> LoadAsync(Guid requestId, CancellationToken cancellationToken = default) => Task.FromResult<RequestTrace?>(null);
        public Task<RequestTraceSearchResult> SearchAsync(RequestTraceSearchCriteria criteria, CancellationToken cancellationToken = default) => Task.FromResult(new RequestTraceSearchResult([], 0, 1, 50));
        public Task<RequestTracePurgeResult> PurgeAsync(DateTime? beforeUtc, CancellationToken cancellationToken = default) => Task.FromResult(new RequestTracePurgeResult(0));
        public Task<int> PurgeLegacyUiEventsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<RequestTraceSettings?> LoadSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult(StoredSettings);
        public Task StoreSettingsAsync(RequestTraceSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
