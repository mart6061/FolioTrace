using Services;

namespace API;

public sealed class AggregateMaintenanceHostedService(
    AggregateMaintenanceOptions options,
    AggregateMaintenanceCoordinator coordinator,
    ApiReadinessState readinessState) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await readinessState.WaitUntilReadyAsync(stoppingToken);

        if (!options.Enabled)
            return;

        // Runs once immediately, before the periodic timer's first tick, so a fresh process doesn't serve
        // PeriodicDelay's worth of cold rebuilds after every restart. For HoldingPositions specifically, this
        // is what makes the warm loop pull from persisted snapshots on startup (Aggregate-Snapshot-Scaling-
        // Plan.md 3.5) instead of only ever seeding the cache after the first periodic run.
        await coordinator.RunAsync("Startup", stoppingToken);

        if (options.PeriodicDelay <= TimeSpan.Zero)
            return;

        using var timer = new PeriodicTimer(options.PeriodicDelay);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await coordinator.RunAsync("Periodic", stoppingToken);
    }
}

