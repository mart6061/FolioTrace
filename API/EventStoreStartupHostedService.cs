using Microsoft.Extensions.Options;
using Repository;

namespace API;

public sealed class EventStoreStartupHostedService(
    IEventRepository eventRepository,
    ApiReadinessState readinessState,
    IOptions<ApiReadinessOptions> options,
    ILogger<EventStoreStartupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retryDelay = TimeSpan.FromSeconds(Math.Max(1, options.Value.StartupRetrySeconds));

        while (!stoppingToken.IsCancellationRequested && !readinessState.Ready)
        {
            try
            {
                logger.LogInformation("Loading events from the event store.");
                await eventRepository.InitializeAsync(stoppingToken);
                readinessState.MarkReady();
                logger.LogInformation("API startup is complete and the API is ready.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "API startup failed. Retrying in {RetryDelaySeconds} seconds.", retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, stoppingToken);
            }
        }
    }
}
