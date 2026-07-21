using JasperFx;
using Marten;
using Repository;
using Services;

namespace API;

public sealed class BuildCoordinator(
    IServiceScopeFactory serviceScopeFactory,
    AggregateCacheClearService cacheClearService,
    AggregateUpdateNotificationService notificationService,
    AggregateMaintenanceCoordinator aggregateMaintenanceCoordinator,
    IDocumentStore documentStore,
    ApiReadinessState readinessState,
    IHostApplicationLifetime applicationLifetime,
    ILogger<BuildCoordinator> logger)
{
    private const int CoordinatorStepOffset = 1;
    private const int TotalCoordinatorSteps = 14;

    private readonly SemaphoreSlim buildLock = new(1, 1);
    private BuildProgressNotification? currentBuild;

    public BuildCoordinatorStartResult TryStartBuild()
    {
        if (!buildLock.Wait(0))
        {
            var rejected = CreateRejectedNotification();
            notificationService.PublishBuildProgress(rejected);
            return new BuildCoordinatorStartResult(false, rejected);
        }

        var buildID = Guid.CreateGuid7();
        var startedAtUtc = DateTime.UtcNow;
        readinessState.MarkNotReady();
        var starting = Publish(buildID, "Running", "Starting", "Starting database rebuild.", 0, 0, 0, startedAtUtc, null);

        _ = RunBuildAsync(buildID, startedAtUtc, applicationLifetime.ApplicationStopping);
        return new BuildCoordinatorStartResult(true, starting);
    }

    private async Task RunBuildAsync(Guid buildID, DateTime startedAtUtc, CancellationToken cancellationToken)
    {
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

            Publish(buildID, "Running", "Schema", "Applying configured database indexes.", TotalCoordinatorSteps - 3, currentBuild?.CompletedEvents ?? 0, currentBuild?.TotalEvents ?? 0, startedAtUtc, null);
            await documentStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync(AutoCreate.CreateOrUpdate);
            Publish(buildID, "Running", "Schema", "Configured database indexes applied.", TotalCoordinatorSteps - 2, currentBuild?.CompletedEvents ?? 0, currentBuild?.TotalEvents ?? 0, startedAtUtc, null);

            var requestTraceRepository = scope.ServiceProvider.GetRequiredService<IRequestTraceRepository>();
            Publish(buildID, "Running", "Diagnostics", "Removing legacy UI request trace events.", TotalCoordinatorSteps - 2, currentBuild?.CompletedEvents ?? 0, currentBuild?.TotalEvents ?? 0, startedAtUtc, null);
            var removedUiTraceEvents = await requestTraceRepository.PurgeLegacyUiEventsAsync(cancellationToken);
            Publish(buildID, "Running", "Diagnostics", $"Removed {removedUiTraceEvents} legacy UI request trace events.", TotalCoordinatorSteps - 1, currentBuild?.CompletedEvents ?? 0, currentBuild?.TotalEvents ?? 0, startedAtUtc, null);

            Publish(buildID, "Running", "Cache", "Clearing aggregate caches after rebuild.", TotalCoordinatorSteps - 1, currentBuild?.CompletedEvents ?? 0, currentBuild?.TotalEvents ?? 0, startedAtUtc, null);
            cacheClearService.ClearAll();
            notificationService.PublishAggregatesInvalidated("Database rebuild complete.");
            readinessState.MarkReady();
            Publish(buildID, "Succeeded", "Complete", "Database rebuild complete.", TotalCoordinatorSteps, currentBuild?.CompletedEvents ?? 0, currentBuild?.TotalEvents ?? 0, startedAtUtc, null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database rebuild {BuildID} failed during {Stage}.", buildID, currentBuild?.Stage ?? "Unknown");
            Publish(buildID, "Failed", "Failed", "Database rebuild failed.", currentBuild?.CompletedSteps ?? 0, currentBuild?.CompletedEvents ?? 0, currentBuild?.TotalEvents ?? 0, startedAtUtc, exception.Message);
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
            current?.BuildID ?? Guid.CreateGuid7(),
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

public sealed record BuildCoordinatorStartResult(bool Accepted, BuildProgressNotification Progress);

internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
