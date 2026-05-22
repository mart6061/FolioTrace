using Services;

namespace API;

public sealed class AggregateMaintenanceHostedService(
    AggregateMaintenanceOptions options,
    AggregateMaintenanceCoordinator coordinator) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled || options.PeriodicDelay <= TimeSpan.Zero)
            return;

        using var timer = new PeriodicTimer(options.PeriodicDelay);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await coordinator.RunAsync("Periodic", stoppingToken);
    }
}

