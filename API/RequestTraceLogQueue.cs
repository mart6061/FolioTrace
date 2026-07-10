using System.Threading.Channels;
using Repository;

namespace API;

public sealed class RequestTraceLogQueue
{
    private readonly Channel<RequestTraceEvent> channel = Channel.CreateUnbounded<RequestTraceEvent>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public bool TryEnqueue(RequestTraceEvent traceEvent) => channel.Writer.TryWrite(traceEvent);

    public IAsyncEnumerable<RequestTraceEvent> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class RequestTraceLogBackgroundService(
    RequestTraceLogQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<RequestTraceLogBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var traceEvent in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRequestTraceRepository>();
                await repository.AppendAsync(traceEvent, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to persist request trace log event for request {RequestId}.", traceEvent.RequestId);
            }
        }
    }
}
