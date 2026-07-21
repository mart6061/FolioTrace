using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using API.Auth;
using API.FoleoTrader;
using API.TradeFiles;
using Repository;
using Services;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace API;

public static partial class ApiEndpointRegistration
{
    private static void MapNotificationEndpoints(this RouteGroupBuilder api)
    {
        var notifications = api.MapGroup("/Notifications").WithTags("Notifications");

        notifications.MapGet("/Aggregates", async (HttpContext context, AggregateUpdateNotificationService notificationService) =>
        {
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.ContentType = "text/event-stream";

            await context.Response.WriteAsync(": connected\n\n", context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);

            await using var subscription = notificationService.Subscribe();

            try
            {
                while (!context.RequestAborted.IsCancellationRequested)
                {
                    var readTask = subscription.Reader.WaitToReadAsync(context.RequestAborted).AsTask();
                    var heartbeatTask = Task.Delay(TimeSpan.FromSeconds(20), context.RequestAborted);
                    var completedTask = await Task.WhenAny(readTask, heartbeatTask);

                    if (completedTask == heartbeatTask)
                    {
                        await context.Response.WriteAsync(": heartbeat\n\n", context.RequestAborted);
                        await context.Response.Body.FlushAsync(context.RequestAborted);

                        continue;
                    }

                    if (!await readTask)
                        break;

                    while (subscription.Reader.TryRead(out var notification))
                    {
                        await WriteSseNotification(context, notification);
                    }
                }
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
            }
        });
    }

    private static async Task WriteSseNotification(HttpContext context, object notification)
    {
        var eventName = notification switch
        {
            BuildProgressNotification => "build-progress",
            AggregateMaintenanceNotification => "aggregate-maintenance",
            AggregateUpdateNotification aggregateNotification when aggregateNotification.NotificationType == "AggregatesInvalidated" => "aggregates-invalidated",
            AggregateUpdateNotification => "aggregate-updated",
            _ => "message"
        };

        var payload = JsonSerializer.Serialize(notification, NotificationJsonOptions);
        await context.Response.WriteAsync($"event: {eventName}\n", context.RequestAborted);
        await context.Response.WriteAsync($"data: {payload}\n\n", context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }

    private static void MapDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        var diagnostics = api.MapGroup("/Diagnostics").WithTags("Diagnostics");

        diagnostics.MapGet("/Memory", (IEventRepository eventRepository, AccountService accountService, BrokerService brokerService, CountryService countryService, CurrencyService currencyService, FXService fxService, FXRateService fxRateService, HoldingService holdingService, HoldingPositionService holdingPositionService, InstrumentService instrumentService, InstrumentValueService instrumentValueService, UserService userService, AggregateUpdateNotificationService notificationService, AggregateMaintenanceCoordinator aggregateMaintenanceCoordinator) =>
        {
            var repositoryDiagnostics = eventRepository.GetCacheDiagnostics();
            var accountDiagnostics = accountService.GetDiagnostics();
            var brokerDiagnostics = brokerService.GetDiagnostics();
            var countryDiagnostics = countryService.GetDiagnostics();
            var currencyDiagnostics = currencyService.GetDiagnostics();
            var fxDiagnostics = fxService.GetDiagnostics();
            var fxRateDiagnostics = fxRateService.GetDiagnostics();
            var holdingDiagnostics = holdingService.GetDiagnostics();
            var holdingPositionDiagnostics = holdingPositionService.GetDiagnostics();
            var instrumentDiagnostics = instrumentService.GetDiagnostics();
            var instrumentValueDiagnostics = instrumentValueService.GetDiagnostics();
            var userDiagnostics = userService.GetDiagnostics();
            var sseDiagnostics = notificationService.GetDiagnostics();
            var aggregateMaintenanceDiagnostics = aggregateMaintenanceCoordinator.GetDiagnostics();

            return Results.Ok(new MemoryDiagnosticsResponse(
                new EventCacheDiagnosticsResponse(
                    repositoryDiagnostics.IsLoaded,
                    repositoryDiagnostics.StreamCount,
                    repositoryDiagnostics.EventCount,
                    repositoryDiagnostics.EstimatedMemoryBytes,
                    repositoryDiagnostics.UnprocessedEventCount,
                    repositoryDiagnostics.RecentUnprocessedEvents
                        .Select(@event => new UnprocessedEventDiagnosticsResponse(@event.StreamId, @event.EventId, @event.EventType, @event.Reason, @event.RecordedAtUtc))
                        .ToList()),
                new AccountServiceDiagnosticsResponse(
                    accountDiagnostics.CacheEntryCount,
                    accountDiagnostics.AccountCount,
                    accountDiagnostics.EstimatedMemoryBytes),
                new BrokerServiceDiagnosticsResponse(
                    brokerDiagnostics.CacheEntryCount,
                    brokerDiagnostics.BrokerCount,
                    brokerDiagnostics.EstimatedMemoryBytes),
                new CountryServiceDiagnosticsResponse(
                    countryDiagnostics.CacheEntryCount,
                    countryDiagnostics.CountryCount,
                    countryDiagnostics.EstimatedMemoryBytes),
                new CurrencyServiceDiagnosticsResponse(
                    currencyDiagnostics.CacheEntryCount,
                    currencyDiagnostics.CurrencyCount,
                    currencyDiagnostics.EstimatedMemoryBytes),
                new FXServiceDiagnosticsResponse(
                    fxDiagnostics.CacheEntryCount,
                    fxDiagnostics.FXCount,
                    fxDiagnostics.EstimatedMemoryBytes),
                new FXRateServiceDiagnosticsResponse(
                    fxRateDiagnostics.CacheEntryCount,
                    fxRateDiagnostics.FXRateCount,
                    fxRateDiagnostics.EstimatedMemoryBytes),
                new HoldingServiceDiagnosticsResponse(
                    holdingDiagnostics.CacheEntryCount,
                    holdingDiagnostics.HoldingCount,
                    holdingDiagnostics.EstimatedMemoryBytes),
                new HoldingPositionServiceDiagnosticsResponse(
                    holdingPositionDiagnostics.CacheEntryCount,
                    holdingPositionDiagnostics.PositionCount,
                    holdingPositionDiagnostics.EstimatedMemoryBytes,
                    holdingPositionDiagnostics.SnapshotVerifiedCount,
                    holdingPositionDiagnostics.SnapshotMismatchCount,
                    holdingPositionDiagnostics.LastSnapshotMismatchAtUtc,
                    holdingPositionDiagnostics.LastSnapshotMismatchDetails),
                new InstrumentServiceDiagnosticsResponse(
                    instrumentDiagnostics.CacheEntryCount,
                    instrumentDiagnostics.InstrumentCount,
                    instrumentDiagnostics.EstimatedMemoryBytes),
                new InstrumentValueServiceDiagnosticsResponse(
                    instrumentValueDiagnostics.CacheEntryCount,
                    instrumentValueDiagnostics.InstrumentValueCount,
                    instrumentValueDiagnostics.EstimatedMemoryBytes),
                new UserServiceDiagnosticsResponse(
                    userDiagnostics.CacheEntryCount,
                    userDiagnostics.UserCount,
                    userDiagnostics.EstimatedMemoryBytes),
                new SseDiagnosticsResponse(
                    sseDiagnostics.ActiveSubscriberCount,
                    sseDiagnostics.PublishedNotificationCount,
                    sseDiagnostics.LastNotificationType,
                    sseDiagnostics.LastKind,
                    sseDiagnostics.LastEventID,
                    sseDiagnostics.LastEventDateTime,
                    sseDiagnostics.LastAuditDateTime,
                    sseDiagnostics.LastReason,
                    sseDiagnostics.CurrentBuildID,
                    sseDiagnostics.LastBuildStatus,
                    sseDiagnostics.LastBuildStage,
                    sseDiagnostics.LastBuildUpdatedAtUtc),
                new AggregateMaintenanceDiagnosticsResponse(
                    aggregateMaintenanceDiagnostics.Enabled,
                    aggregateMaintenanceDiagnostics.PeriodicDelay,
                    aggregateMaintenanceDiagnostics.EventTriggerCount,
                    aggregateMaintenanceDiagnostics.EventTriggerDelay,
                    aggregateMaintenanceDiagnostics.DaysFromToday,
                    aggregateMaintenanceDiagnostics.EndOfWeeksFromToday,
                    aggregateMaintenanceDiagnostics.EndOfMonthsFromToday,
                    aggregateMaintenanceDiagnostics.Status,
                    aggregateMaintenanceDiagnostics.ActiveRunID,
                    aggregateMaintenanceDiagnostics.LastRunID,
                    aggregateMaintenanceDiagnostics.LastTrigger,
                    aggregateMaintenanceDiagnostics.LastStartedAtUtc,
                    aggregateMaintenanceDiagnostics.LastCompletedAtUtc,
                    aggregateMaintenanceDiagnostics.LastScannedAggregates,
                    aggregateMaintenanceDiagnostics.LastMissingAggregates,
                    aggregateMaintenanceDiagnostics.LastFixedAggregates,
                    aggregateMaintenanceDiagnostics.LastFailedAggregates,
                    aggregateMaintenanceDiagnostics.TotalScannedAggregates,
                    aggregateMaintenanceDiagnostics.TotalMissingAggregates,
                    aggregateMaintenanceDiagnostics.TotalFixedAggregates,
                    aggregateMaintenanceDiagnostics.TotalFailedAggregates,
                    aggregateMaintenanceDiagnostics.SkippedRunCount,
                    aggregateMaintenanceDiagnostics.PendingEventCount,
                    aggregateMaintenanceDiagnostics.IsSuspended,
                    aggregateMaintenanceDiagnostics.SuspensionReason,
                    aggregateMaintenanceDiagnostics.SuspendedAtUtc,
                    aggregateMaintenanceDiagnostics.SuspendedRunCount,
                    aggregateMaintenanceDiagnostics.LastError,
                    aggregateMaintenanceDiagnostics.RecentErrors)));
        });

        diagnostics.MapGet("/AggregateMaintenance", (AggregateMaintenanceCoordinator aggregateMaintenanceCoordinator) =>
        {
            var aggregateMaintenanceDiagnostics = aggregateMaintenanceCoordinator.GetDiagnostics();

            return Results.Ok(new AggregateMaintenanceDiagnosticsResponse(
                aggregateMaintenanceDiagnostics.Enabled,
                aggregateMaintenanceDiagnostics.PeriodicDelay,
                aggregateMaintenanceDiagnostics.EventTriggerCount,
                aggregateMaintenanceDiagnostics.EventTriggerDelay,
                aggregateMaintenanceDiagnostics.DaysFromToday,
                aggregateMaintenanceDiagnostics.EndOfWeeksFromToday,
                aggregateMaintenanceDiagnostics.EndOfMonthsFromToday,
                aggregateMaintenanceDiagnostics.Status,
                aggregateMaintenanceDiagnostics.ActiveRunID,
                aggregateMaintenanceDiagnostics.LastRunID,
                aggregateMaintenanceDiagnostics.LastTrigger,
                aggregateMaintenanceDiagnostics.LastStartedAtUtc,
                aggregateMaintenanceDiagnostics.LastCompletedAtUtc,
                aggregateMaintenanceDiagnostics.LastScannedAggregates,
                aggregateMaintenanceDiagnostics.LastMissingAggregates,
                aggregateMaintenanceDiagnostics.LastFixedAggregates,
                aggregateMaintenanceDiagnostics.LastFailedAggregates,
                aggregateMaintenanceDiagnostics.TotalScannedAggregates,
                aggregateMaintenanceDiagnostics.TotalMissingAggregates,
                aggregateMaintenanceDiagnostics.TotalFixedAggregates,
                aggregateMaintenanceDiagnostics.TotalFailedAggregates,
                aggregateMaintenanceDiagnostics.SkippedRunCount,
                aggregateMaintenanceDiagnostics.PendingEventCount,
                aggregateMaintenanceDiagnostics.IsSuspended,
                aggregateMaintenanceDiagnostics.SuspensionReason,
                aggregateMaintenanceDiagnostics.SuspendedAtUtc,
                aggregateMaintenanceDiagnostics.SuspendedRunCount,
                aggregateMaintenanceDiagnostics.LastError,
                aggregateMaintenanceDiagnostics.RecentErrors));
        });

        diagnostics.MapGet("/RequestTrace", async (
            DateTime? fromUtc,
            DateTime? toUtc,
            string? method,
            string? path,
            int? statusCode,
            int? minimumDurationMilliseconds,
            int? maximumDurationMilliseconds,
            string? text,
            int? page,
            int? pageSize,
            IRequestTraceRepository repository,
            RequestTraceLogQueue queue,
            RequestTraceSettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await repository.SearchAsync(
                    new RequestTraceSearchCriteria(
                        fromUtc,
                        toUtc,
                        method,
                        path,
                        statusCode,
                        minimumDurationMilliseconds,
                        maximumDurationMilliseconds,
                        text,
                        page ?? 1,
                        pageSize ?? 50),
                    cancellationToken);
                var settings = await settingsService.GetAsync(cancellationToken);

                return Results.Ok(new RequestTraceSearchResponse(
                    result.Items.Select(ToResponse).ToList(),
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    ToResponse(settings),
                    new RequestTraceQueueDiagnosticsResponse(queue.Capacity, queue.DroppedEventCount)));
            }
            catch (Exception exception) when (IsRequestTraceStoreUnavailable(exception))
            {
                return RequestTraceUnavailable();
            }
        });

        diagnostics.MapGet("/RequestTrace/Settings", async (RequestTraceSettingsService settingsService, CancellationToken cancellationToken) =>
            Results.Ok(ToResponse(await settingsService.GetAsync(cancellationToken))));

        diagnostics.MapPut("/RequestTrace/Settings", async (
            RequestTraceSettingsRequest request,
            RequestTraceSettingsService settingsService,
            CancellationToken cancellationToken) =>
            Results.Ok(ToResponse(await settingsService.UpdateAsync(ToSettings(request), cancellationToken))));

        diagnostics.MapPost("/RequestTrace/Purge", async (
            RequestTracePurgeRequest request,
            IRequestTraceRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (!string.Equals(request.Confirmation, "Purge", StringComparison.Ordinal))
                return Results.BadRequest(new { message = "Confirmation must be exactly 'Purge'." });

            try
            {
                var result = await repository.PurgeAsync(request.BeforeUtc, cancellationToken);
                return Results.Ok(new RequestTracePurgeResponse(result.DeletedCount));
            }
            catch (Exception exception) when (IsRequestTraceStoreUnavailable(exception))
            {
                return RequestTraceUnavailable();
            }
        });

        diagnostics.MapPost("/RequestTrace/Events", async (
            RequestTraceEventIngestRequest request,
            RequestTraceLogQueue queue,
            RequestTraceSettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            if (!settings.Enabled || !settings.CaptureUi)
                return Results.Accepted();

            queue.TryEnqueue(ToTraceEvent(request));
            return Results.Accepted();
        });

        diagnostics.MapGet("/RequestTrace/{requestId:guid}", async (Guid requestId, IRequestTraceRepository repository, CancellationToken cancellationToken) =>
        {
            RequestTrace? trace;

            try
            {
                trace = await repository.LoadAsync(requestId, cancellationToken);
            }
            catch (Exception exception) when (IsRequestTraceStoreUnavailable(exception))
            {
                return RequestTraceUnavailable();
            }

            return trace is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(trace));
        });

        diagnostics.MapGet("/FIXTrace", async (
            DateTime? fromUtc,
            DateTime? toUtc,
            string? direction,
            string? channel,
            string? msgType,
            string? clOrdID,
            string? execID,
            string? text,
            int? page,
            int? pageSize,
            IFoleoTraderFixOperationRepository repository,
            CancellationToken cancellationToken) =>
        {
            var result = await repository.SearchAsync(new FoleoTraderFixOperationSearchCriteria(
                fromUtc,
                toUtc,
                direction,
                channel,
                msgType,
                clOrdID,
                execID,
                text,
                page ?? 1,
                pageSize ?? 50), cancellationToken);

            return Results.Ok(new FIXOperationSearchResponse(
                result.Items.Select(ToResponse).ToList(),
                result.TotalCount,
                result.Page,
                result.PageSize));
        });
    }

    private static void MapSystemEndpoints(this RouteGroupBuilder api)
    {
        var system = api.MapGroup("/System").WithTags("System");

        system.MapGet("/Version", (ApiVersionInfo versionInfo) =>
        {
            return Results.Ok(new
            {
                versionInfo.ApiVersion
            });
        });

        system.MapPost("/Build", async (
            BuildCoordinator buildCoordinator,
            CancellationToken cancellationToken) =>
        {
            var result = await buildCoordinator.BuildAsync(cancellationToken);

            if (!result.Accepted)
                return Results.Conflict(new
                {
                    Status = result.Progress.Status,
                    Message = result.Progress.Message,
                    Progress = result.Progress
                });

            if (!result.Succeeded)
                return Results.Problem(result.Progress.Error ?? result.Progress.Message, statusCode: StatusCodes.Status500InternalServerError);

            return Results.Ok(new
            {
                Status = "Complete",
                Message = "Database rebuild complete.",
                RemovedCacheViews = result.RemovedCacheViews,
                Progress = result.Progress
            });
        });

        system.MapPost("/ClearCacheAndProjections", async (
            AggregateCacheClearService cacheClearService,
            AggregateUpdateNotificationService notificationService,
            AggregateMaintenanceCoordinator aggregateMaintenanceCoordinator,
            IFXRateReadModelRepository fxRateReadModelRepository,
            CancellationToken cancellationToken) =>
        {
            await using var maintenanceSuspension = await aggregateMaintenanceCoordinator.SuspendAsync("Clearing caches and projections.", cancellationToken);

            var removedCacheViews = cacheClearService.ClearAll();
            await fxRateReadModelRepository.ClearAsync(cancellationToken);
            notificationService.PublishAggregatesInvalidated("Caches and projections cleared.");

            return Results.Ok(new
            {
                Status = "Complete",
                Message = "Caches and projections cleared.",
                RemovedCacheViews = removedCacheViews,
                ClearedProjections = new[]
                {
                    nameof(FXDefinitionReadModel),
                    nameof(FXRatePointReadModel)
                }
            });
        });
    }

}
