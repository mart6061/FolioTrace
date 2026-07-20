using API;
using API.FoleoTrader;
using Microsoft.Extensions.Logging;

namespace Test;

public sealed class FixStartupHealthStateTests
{
    [Fact]
    public async Task StartupFailure_IsObservedLoggedAndReportedAsFailed()
    {
        var health = new FixStartupHealthState();
        var logger = new RecordingLogger();

        await FoleoTraderFixClient.RunStartupAsync(
            _ => throw new InvalidOperationException("Replay failed."),
            health,
            logger,
            CancellationToken.None);

        Assert.Equal(FixStartupStatus.Failed, health.Snapshot.Status);
        Assert.Equal("Replay failed.", health.Snapshot.FailureMessage);
        Assert.NotNull(health.Snapshot.FailedAtUtc);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Critical && entry.Exception is InvalidOperationException);
    }

    [Fact]
    public async Task SuccessfulStartup_IsReportedAsReady()
    {
        var health = new FixStartupHealthState();

        await FoleoTraderFixClient.RunStartupAsync(
            _ => Task.CompletedTask,
            health,
            new RecordingLogger(),
            CancellationToken.None);

        Assert.Equal(FixStartupStatus.Ready, health.Snapshot.Status);
        Assert.Null(health.Snapshot.FailureMessage);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, exception));
    }
}
