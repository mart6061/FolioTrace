using API;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Test;

public sealed class ApiRequestLoggingMiddlewareTests
{
    [Fact]
    public async Task FastSuccessfulRequest_DoesNotWriteCompletionLog()
    {
        var logger = new RecordingLogger<ApiRequestLoggingMiddleware>();
        var middleware = CreateMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger,
            500);

        await middleware.InvokeAsync(CreateContext());

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task SlowSuccessfulRequest_WritesWarningCompletionLog()
    {
        var logger = new RecordingLogger<ApiRequestLoggingMiddleware>();
        var middleware = CreateMiddleware(
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                await Task.Delay(20);
            },
            logger,
            1);

        await middleware.InvokeAsync(CreateContext());

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, LogLevel.Warning)]
    [InlineData(StatusCodes.Status404NotFound, LogLevel.Warning)]
    [InlineData(StatusCodes.Status500InternalServerError, LogLevel.Error)]
    public async Task FailedRequest_WritesExpectedCompletionLog(int statusCode, LogLevel expectedLevel)
    {
        var logger = new RecordingLogger<ApiRequestLoggingMiddleware>();
        var middleware = CreateMiddleware(
            context =>
            {
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            },
            logger,
            500);

        await middleware.InvokeAsync(CreateContext());

        Assert.Contains(logger.Entries, entry => entry.Level == expectedLevel);
    }

    private static ApiRequestLoggingMiddleware CreateMiddleware(
        RequestDelegate next,
        ILogger<ApiRequestLoggingMiddleware> logger,
        double thresholdMilliseconds) =>
        new(
            next,
            logger,
            Options.Create(new ApiObservabilityOptions
            {
                SlowRequestThresholdMilliseconds = thresholdMilliseconds
            }));

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.PathBase = "/API";
        context.Request.Path = "/test";
        return context;
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
