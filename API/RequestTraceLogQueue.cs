using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Repository;

namespace API;

public sealed class RequestTraceLogQueue
{
    private readonly Channel<RequestTraceEvent> channel;
    private readonly ILogger<RequestTraceLogQueue> logger;
    private long droppedEventCount;

    public RequestTraceLogQueue(IOptions<RequestTraceOptions> options, ILogger<RequestTraceLogQueue> logger)
    {
        this.logger = logger;
        Capacity = Math.Max(1, options.Value.QueueCapacity);
        channel = Channel.CreateBounded<RequestTraceEvent>(
        new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        }, OnDropped);
    }

    public int Capacity { get; }

    public long DroppedEventCount => Interlocked.Read(ref droppedEventCount);

    public bool TryEnqueue(RequestTraceEvent traceEvent) => channel.Writer.TryWrite(traceEvent);

    public IAsyncEnumerable<RequestTraceEvent> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);

    internal bool TryDequeue(out RequestTraceEvent? traceEvent) => channel.Reader.TryRead(out traceEvent);

    private void OnDropped(RequestTraceEvent traceEvent)
    {
        var count = Interlocked.Increment(ref droppedEventCount);
        if (count == 1 || count % 100 == 0)
            logger.LogWarning("Request trace queue dropped {DroppedEventCount} events because its capacity of {Capacity} was reached. Latest dropped request: {RequestId}.", count, Capacity, traceEvent.RequestId);
    }
}

public sealed class RequestTraceLogBackgroundService(
    RequestTraceLogQueue queue,
    IServiceScopeFactory scopeFactory,
    ApiReadinessState readinessState,
    ILogger<RequestTraceLogBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await readinessState.WaitUntilReadyAsync(stoppingToken);

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
