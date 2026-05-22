using Repository;
using Services;

namespace API;

public sealed class BuildCoordinator(
    IServiceScopeFactory serviceScopeFactory,
    AggregateCacheClearService cacheClearService,
    AggregateUpdateNotificationService notificationService,
    AggregateMaintenanceCoordinator aggregateMaintenanceCoordinator)
{
    private const int CoordinatorStepOffset = 1;
    private const int TotalCoordinatorSteps = 12;

    private readonly SemaphoreSlim buildLock = new(1, 1);
    private BuildProgressNotification? currentBuild;

    public async Task<BuildCoordinatorResult> BuildAsync(CancellationToken cancellationToken)
    {
        if (!await buildLock.WaitAsync(0, cancellationToken))
        {
            var rejected = CreateRejectedNotification();
            notificationService.PublishBuildProgress(rejected);
            return BuildCoordinatorResult.Rejected(rejected);
        }

        var buildID = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;

        try
        {
            await using var maintenanceSuspension = await aggregateMaintenanceCoordinator.SuspendAsync("Database rebuild running.", cancellationToken);

            Publish(buildID, "Running", "Cache", "Clearing aggregate caches before rebuild.", 0, 0, 0, startedAtUtc, null);
            cacheClearService.ClearAll();
            Publish(buildID, "Running", "Cache", "Aggregate caches cleared.", 1, 0, 0, startedAtUtc, null);

            var progress = new InlineProgress<BuildProgress>(progress =>
            {
                Publish(
                    buildID,
                    "Running",
                    progress.Stage,
                    progress.Message,
                    progress.CompletedSteps + CoordinatorStepOffset,
                    progress.CompletedEvents,
                    progress.TotalEvents,
                    startedAtUtc,
                    null);
            });

            using var scope = serviceScopeFactory.CreateScope();
            var seedRepository = scope.ServiceProvider.GetRequiredService<ISeedRepository>();

            await seedRepository.Build(progress, cancellationToken);

            Publish(buildID, "Running", "Cache", "Clearing aggregate caches after rebuild.", TotalCoordinatorSteps - 1, currentBuild?.CompletedEvents ?? 0, currentBuild?.TotalEvents ?? 0, startedAtUtc, null);
            var removedCacheViews = cacheClearService.ClearAll();
            notificationService.PublishAggregatesInvalidated("Database rebuild complete.");
            var completed = Publish(buildID, "Succeeded", "Complete", "Database rebuild complete.", TotalCoordinatorSteps, currentBuild?.CompletedEvents ?? 0, currentBuild?.TotalEvents ?? 0, startedAtUtc, null);

            return BuildCoordinatorResult.Completed(completed, removedCacheViews);
        }
        catch (Exception exception)
        {
            var failed = Publish(buildID, "Failed", "Failed", "Database rebuild failed.", currentBuild?.CompletedSteps ?? 0, currentBuild?.CompletedEvents ?? 0, currentBuild?.TotalEvents ?? 0, startedAtUtc, exception.Message);
            return BuildCoordinatorResult.Failed(failed);
        }
        finally
        {
            buildLock.Release();
        }
    }

    private BuildProgressNotification Publish(
        Guid buildID,
        string status,
        string stage,
        string message,
        int completedSteps,
        int completedEvents,
        int totalEvents,
        DateTime startedAtUtc,
        string? error)
    {
        var notification = new BuildProgressNotification(
            "BuildProgress",
            buildID,
            status,
            stage,
            message,
            completedSteps,
            TotalCoordinatorSteps,
            completedEvents,
            totalEvents,
            startedAtUtc,
            DateTime.UtcNow,
            error);

        currentBuild = notification;
        notificationService.PublishBuildProgress(notification);
        return notification;
    }

    private BuildProgressNotification CreateRejectedNotification()
    {
        var current = currentBuild;
        return new BuildProgressNotification(
            "BuildProgress",
            current?.BuildID ?? Guid.NewGuid(),
            "Rejected",
            current?.Stage ?? "Build",
            "A database build is already running.",
            current?.CompletedSteps ?? 0,
            current?.TotalSteps ?? TotalCoordinatorSteps,
            current?.CompletedEvents ?? 0,
            current?.TotalEvents ?? 0,
            current?.StartedAtUtc ?? DateTime.UtcNow,
            DateTime.UtcNow,
            "A database build is already running.");
    }
}

public sealed record BuildCoordinatorResult(
    bool Accepted,
    bool Succeeded,
    BuildProgressNotification Progress,
    AggregateCacheClearResult? RemovedCacheViews)
{
    public static BuildCoordinatorResult Completed(BuildProgressNotification progress, AggregateCacheClearResult removedCacheViews) =>
        new(true, true, progress, removedCacheViews);

    public static BuildCoordinatorResult Failed(BuildProgressNotification progress) =>
        new(true, false, progress, null);

    public static BuildCoordinatorResult Rejected(BuildProgressNotification progress) =>
        new(false, false, progress, null);
}

internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
