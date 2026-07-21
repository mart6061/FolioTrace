using System.Text.Json;
using API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Repository;

namespace Test;

public sealed class ApiUnhandledExceptionLoggingMiddlewareTests
{
    [Fact]
    public async Task DownstreamException_ReturnsStructuredProblemJson()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"foliotrace-api-errors-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRoot);

        try
        {
            var repository = new StubRequestTraceRepository();
            var services = new ServiceCollection()
                .AddSingleton<IRequestTraceRepository>(repository)
                .BuildServiceProvider();
            var settings = new RequestTraceSettingsService(
                services.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new RequestTraceOptions { Enabled = false }),
                NullLogger<RequestTraceSettingsService>.Instance);
            var middleware = new ApiUnhandledExceptionLoggingMiddleware(
                _ => throw new InvalidOperationException("Sensitive failure."),
                new TestWebHostEnvironment { ContentRootPath = contentRoot },
                NullLogger<ApiUnhandledExceptionLoggingMiddleware>.Instance,
                new ApiUnhandledExceptionLoggingOptions());
            var queue = new RequestTraceLogQueue(
                Options.Create(new RequestTraceOptions { QueueCapacity = 16 }),
                NullLogger<RequestTraceLogQueue>.Instance);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Path = "/failure";

            await middleware.InvokeAsync(context, queue, settings);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal("application/problem+json", context.Response.ContentType);
            context.Response.Body.Position = 0;
            using var body = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.Equal(500, body.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("An unexpected error occurred.", body.RootElement.GetProperty("title").GetString());
            Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("detail").ValueKind);
            Assert.True(body.RootElement.TryGetProperty("errorId", out _));
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubRequestTraceRepository : IRequestTraceRepository
    {
        public Task AppendAsync(RequestTraceEvent traceEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AppendAsync(IReadOnlyCollection<RequestTraceEvent> traceEvents, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<RequestTrace?> LoadAsync(Guid requestId, CancellationToken cancellationToken = default) => Task.FromResult<RequestTrace?>(null);
        public Task<RequestTraceSearchResult> SearchAsync(RequestTraceSearchCriteria criteria, CancellationToken cancellationToken = default) => Task.FromResult(new RequestTraceSearchResult([], 0, 1, 50));
        public Task<RequestTracePurgeResult> PurgeAsync(DateTime? beforeUtc, CancellationToken cancellationToken = default) => Task.FromResult(new RequestTracePurgeResult(0));
        public Task<int> PurgeLegacyUiEventsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<RequestTraceSettings?> LoadSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult<RequestTraceSettings?>(null);
        public Task StoreSettingsAsync(RequestTraceSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
