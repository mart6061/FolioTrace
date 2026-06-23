using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using API.FoleoTrader;
using Repository;
using Services;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace API;

public static class ApiEndpointRegistration
{
    private static readonly JsonSerializerOptions NotificationJsonOptions = new(JsonSerializerDefaults.Web);

    private const string AccountEventsRoute = "/API/Events/Account";
    private const string BrokerEventsRoute = "/API/Events/Broker";
    private const string CountryEventsRoute = "/API/Events/Country";
    private const string CurrencyEventsRoute = "/API/Events/Currency";
    private const string FXEventsRoute = "/API/Events/FX";
    private const string FXRateEventsRoute = "/API/Events/FXRate";
    private const string HoldingEventsRoute = "/API/Events/Holding";
    private const string InstrumentEventsRoute = "/API/Events/Instrument";
    private const string InstrumentPriceEventsRoute = "/API/Events/InstrumentPrice";
    private const string InstrumentIncomeEventsRoute = "/API/Events/InstrumentIncome";
    private const string TransactionEventsRoute = "/API/Events/Transaction";
    private const string TicketEventsRoute = "/API/Events/Ticket";
    private const string UserEventsRoute = "/API/Events/User";
    private const string UserMenuPreferencesEventsRoute = "/API/Events/UserMenuPreferences";
    private const string UserValuationPreferencesEventsRoute = "/API/Events/UserValuationPreferences";
    private const string UserBookmarksEventsRoute = "/API/Events/UserBookmarks";
    private const string ValuationSettingEventsRoute = "/API/Events/ValuationSetting";
    private const string AssetAllocationMappingEventsRoute = "/API/Events/AssetAllocationMapping";
    private const string ReportEventsRoute = "/API/Events/Report";

    public static WebApplication MapFolioTraceApi(this WebApplication app)
    {
        var api = app.MapGroup("");

        api.MapDiagnosticsEndpoints();
        api.MapNotificationEndpoints();
        api.MapSystemEndpoints();
        api.MapAccountEndpoints();
        api.MapBrokerEndpoints();
        api.MapCountryEndpoints();
        api.MapCurrencyEndpoints();
        api.MapFXEndpoints();
        api.MapFXRateEndpoints();
        api.MapFoleoTraderEndpoints();
        api.MapHoldingEndpoints();
        api.MapValuationEndpoints();
        api.MapProfitLossEndpoints();
        api.MapValuationSettingEndpoints();
        api.MapAssetAllocationMappingEndpoints();
        api.MapReportConfigEndpoints();
        api.MapInstrumentEndpoints();
        api.MapInstrumentValueEndpoints();
        api.MapTicketEndpoints();
        api.MapUserEndpoints();
        api.MapUserMenuPreferencesEndpoints();
        api.MapUserValuationPreferencesEndpoints();
        api.MapUserBookmarksEndpoints();
        api.MapAccountEventEndpoints();
        api.MapBrokerEventEndpoints();
        api.MapCountryEventEndpoints();
        api.MapCurrencyEventEndpoints();
        api.MapFXEventEndpoints();
        api.MapFXRateEventEndpoints();
        api.MapHoldingEventEndpoints();
        api.MapInstrumentEventEndpoints();
        api.MapInstrumentPriceEventEndpoints();
        api.MapInstrumentIncomeEventEndpoints();
        api.MapTransactionEventEndpoints();
        api.MapTicketEventEndpoints();
        api.MapUserEventEndpoints();
        api.MapUserMenuPreferencesEventEndpoints();
        api.MapUserValuationPreferencesEventEndpoints();
        api.MapUserBookmarksEventEndpoints();
        api.MapValuationSettingEventEndpoints();
        api.MapAssetAllocationMappingEventEndpoints();
        api.MapReportEventEndpoints();

        return app;
    }

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
                    holdingPositionDiagnostics.EstimatedMemoryBytes),
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
            IApiExchangeRepository repository,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await repository.SearchAsync(
                    new ApiExchangeSearchCriteria(
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

                return Results.Ok(new ApiExchangeSearchResponse(
                    result.Items.Select(ToResponse).ToList(),
                    result.TotalCount,
                    result.Page,
                    result.PageSize));
            }
            catch (Exception exception) when (IsApiExchangeStoreUnavailable(exception))
            {
                return RequestTraceUnavailable();
            }
        });

        diagnostics.MapGet("/RequestTrace/{id:guid}", async (Guid id, IApiExchangeRepository repository, CancellationToken cancellationToken) =>
        {
            ApiExchange? exchange;

            try
            {
                exchange = await repository.LoadAsync(id, cancellationToken);
            }
            catch (Exception exception) when (IsApiExchangeStoreUnavailable(exception))
            {
                return RequestTraceUnavailable();
            }

            return exchange is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(exchange));
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
            IEventRepository eventRepository,
            CancellationToken cancellationToken) =>
        {
            var normalizedPage = Math.Max(1, page ?? 1);
            var normalizedPageSize = Math.Clamp(pageSize ?? 50, 1, 200);
            var events = await eventRepository.LoadStreamAsync<FoleoTraderFIXOperationRecordedEvent>(Constants.Initialisation.FoleoTraderFIXOperationsStreamId, cancellationToken);
            var filtered = events
                .Where(@event => !fromUtc.HasValue || @event.AuditDateTime.Value >= fromUtc.Value)
                .Where(@event => !toUtc.HasValue || @event.AuditDateTime.Value <= toUtc.Value)
                .Where(@event => Matches(@event.Direction, direction))
                .Where(@event => Matches(@event.Channel, channel))
                .Where(@event => Matches(@event.MsgType, msgType))
                .Where(@event => Contains(@event.ClOrdID, clOrdID))
                .Where(@event => Contains(@event.ExecID, execID))
                .Where(@event => MatchesFIXText(@event, text))
                .OrderByDescending(@event => @event.AuditDateTime.Value)
                .ThenByDescending(@event => @event.EventID.Value)
                .ToList();

            return Results.Ok(new FIXOperationSearchResponse(
                filtered
                    .Skip((normalizedPage - 1) * normalizedPageSize)
                    .Take(normalizedPageSize)
                    .Select(ToResponse)
                    .ToList(),
                filtered.Count,
                normalizedPage,
                normalizedPageSize));
        });
    }

    private static void MapSystemEndpoints(this RouteGroupBuilder api)
    {
        var system = api.MapGroup("/System").WithTags("System");

        system.MapGet("/Health", () => Results.Ok(new
        {
            Status = "Healthy",
            Service = "FolioTrace API",
            CheckedAtUtc = DateTime.UtcNow
        }));

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

    private static void MapAccountEndpoints(this RouteGroupBuilder api)
    {
        var accounts = api.MapGroup("/Accounts").WithTags("Accounts");

        accounts.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, AccountService accountService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await accountService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await accountService.Get(valuationDate));
        });
    }

    private static void MapBrokerEndpoints(this RouteGroupBuilder api)
    {
        var brokers = api.MapGroup("/Brokers").WithTags("Brokers");

        brokers.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, BrokerService brokerService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await brokerService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await brokerService.Get(valuationDate));
        });
    }

    private static void MapHoldingEndpoints(this RouteGroupBuilder api)
    {
        var holdings = api.MapGroup("/Holdings").WithTags("Holdings");

        holdings.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, Guid? holdingID, Guid? accountID, Guid? instrumentID, string? holdingKind, bool? includeInactive, HoldingService holdingService) =>
        {
            var valuationDateTime = EventDateTimeBuilder.Create(eventDateTime);
            var aggregate = auditDateTime.HasValue
                ? await holdingService.Get(valuationDateTime, AuditDateTimeBuilder.Create(auditDateTime.Value))
                : await holdingService.Get(valuationDateTime);

            var items = aggregate.Items
                .Where(holding => includeInactive == true || holding.Active)
                .Where(holding => !holdingID.HasValue || holding.HoldingID.Value == holdingID.Value)
                .Where(holding => !accountID.HasValue || holding.AccountID.Value == accountID.Value)
                .Where(holding => !instrumentID.HasValue || holding.InstrumentID.Value == instrumentID.Value)
                .Where(holding => string.IsNullOrWhiteSpace(holdingKind) || string.Equals(holding.GetHoldingKindName(), holdingKind, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Results.Ok(aggregate with { Items = items });
        });

        api.MapGet("/HoldingPositions", async (DateTime eventDateTime, DateTime? auditDateTime, HoldingDateBasis? holdingDateBasis, Guid? holdingID, Guid? accountID, Guid? instrumentID, bool? includeExcluded, bool? includeZero, HoldingPositionService holdingPositionService) =>
        {
            var valuationDateTime = EventDateTimeBuilder.Create(eventDateTime);
            var basis = holdingDateBasis ?? HoldingDateBasis.EventDateTime;
            var asAt = auditDateTime.HasValue
                ? AuditDateTimeBuilder.Create(auditDateTime.Value)
                : AuditDateTimeBuilder.Create();
            var filter = new HoldingPositionFilter(
                holdingID.HasValue ? HoldingIDBuilder.Create(holdingID.Value) : null,
                accountID.HasValue ? AccountIDBuilder.Create(accountID.Value) : null,
                instrumentID.HasValue ? InstrumentIDBuilder.Restore(instrumentID.Value) : null,
                includeExcluded == true,
                includeZero == true);

            return Results.Ok(await holdingPositionService.Get(valuationDateTime, asAt, filter, basis));
        }).WithTags("Holdings");
    }

    private static void MapValuationEndpoints(this RouteGroupBuilder api)
    {
        var valuations = api.MapGroup("/Valuations").WithTags("Valuations");

        valuations.MapGet("/", async (
            DateTime eventDateTime,
            DateTime? auditDateTime,
            HoldingDateBasis? holdingDateBasis,
            InstrumentPriceBasis? instrumentPriceBasis,
            string? valuationCurrency,
            Guid? accountID,
            ValuationService valuationService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var asAt = auditDateTime.HasValue
                ? AuditDateTimeBuilder.Create(auditDateTime.Value)
                : AuditDateTimeBuilder.Create();
            var currency = Alpha3Builder.Create(string.IsNullOrWhiteSpace(valuationCurrency) ? "GBP" : valuationCurrency);
            var account = accountID.HasValue ? AccountIDBuilder.Create(accountID.Value) : null;

            return Results.Ok(await valuationService.Get(
                valuationDate,
                asAt,
                holdingDateBasis ?? HoldingDateBasis.EventDateTime,
                instrumentPriceBasis ?? InstrumentPriceBasis.Mid,
                currency,
                account));
        });
    }

    private static void MapProfitLossEndpoints(this RouteGroupBuilder api)
    {
        var profitLoss = api.MapGroup("/ProfitLoss").WithTags("Profit and Loss");

        profitLoss.MapGet("/", async (
            DateTime eventDateTime,
            DateTime? auditDateTime,
            HoldingDateBasis? holdingDateBasis,
            InstrumentPriceBasis? instrumentPriceBasis,
            Guid? accountID,
            ProfitLossService profitLossService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var asAt = auditDateTime.HasValue
                ? AuditDateTimeBuilder.Create(auditDateTime.Value)
                : AuditDateTimeBuilder.Create();
            var account = accountID.HasValue ? AccountIDBuilder.Create(accountID.Value) : null;

            return Results.Ok(await profitLossService.Get(
                valuationDate,
                asAt,
                holdingDateBasis ?? HoldingDateBasis.EventDateTime,
                instrumentPriceBasis ?? InstrumentPriceBasis.Mid,
                account));
        });
    }

    private static void MapValuationSettingEndpoints(this RouteGroupBuilder api)
    {
        var valuationSettings = api.MapGroup("/ValuationSettings").WithTags("Valuation Settings");

        valuationSettings.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, ValuationSettingService valuationSettingService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await valuationSettingService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await valuationSettingService.Get(valuationDate));
        });
    }

    private static void MapAssetAllocationMappingEndpoints(this RouteGroupBuilder api)
    {
        var mappings = api.MapGroup("/AssetAllocationMappings").WithTags("Asset Allocation Mappings");

        mappings.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, Guid? assetAllocationID, Guid? accountID, Guid? holdingID, AssetAllocationMappingService assetAllocationMappingService, HoldingService holdingService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var asAt = auditDateTime.HasValue
                ? AuditDateTimeBuilder.Create(auditDateTime.Value)
                : AuditDateTimeBuilder.Create();
            var aggregate = await assetAllocationMappingService.Get(valuationDate, asAt);

            var items = aggregate.Items
                .Where(mapping => !assetAllocationID.HasValue || mapping.AssetAllocationID.Value == assetAllocationID.Value)
                .Where(mapping => !holdingID.HasValue || mapping.HoldingID.Value == holdingID.Value)
                .ToList();

            if (accountID.HasValue)
            {
                var holdings = await holdingService.Get(valuationDate, asAt);
                var holdingIDSet = holdings.Items
                    .Where(holding => holding.AccountID.Value == accountID.Value)
                    .Select(holding => holding.HoldingID.Value)
                    .ToHashSet();
                items = items
                    .Where(mapping => holdingIDSet.Contains(mapping.HoldingID.Value))
                    .ToList();
            }

            return Results.Ok(aggregate with { Items = items });
        });
    }

    private static void MapReportConfigEndpoints(this RouteGroupBuilder api)
    {
        var reportConfigs = api.MapGroup("/ReportConfigs").WithTags("Report Configs");

        reportConfigs.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, ReportConfigService reportConfigService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await reportConfigService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await reportConfigService.Get(valuationDate));
        });
    }

    private static void MapCountryEndpoints(this RouteGroupBuilder api)
    {
        var countries = api.MapGroup("/Countries").WithTags("Countries");

        countries.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, CountryService countryService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await countryService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await countryService.Get(valuationDate));
        });
    }

    private static void MapCurrencyEndpoints(this RouteGroupBuilder api)
    {
        var currencies = api.MapGroup("/Currencies").WithTags("Currencies");

        currencies.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, CurrencyService currencyService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await currencyService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await currencyService.Get(valuationDate));
        });
    }

    private static void MapFXEndpoints(this RouteGroupBuilder api)
    {
        var fxs = api.MapGroup("/FXs").WithTags("FX");

        fxs.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, FXService fxService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await fxService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await fxService.Get(valuationDate));
        });
    }

    private static void MapFXRateEndpoints(this RouteGroupBuilder api)
    {
        var fxRates = api.MapGroup("/FXRates").WithTags("FX Rates");

        fxRates.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, FXRateService fxRateService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await fxRateService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await fxRateService.Get(valuationDate));
        });
    }

    private static void MapFoleoTraderEndpoints(this RouteGroupBuilder api)
    {
        var foleoTrader = api.MapGroup("/Trading/FoleoTrader").WithTags("FoleoTrader");

        foleoTrader.MapGet("/Orders", async (DateTime eventDateTime, DateTime? auditDateTime, FoleoTraderOrderService foleoTraderOrderService, CancellationToken cancellationToken) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await foleoTraderOrderService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value), cancellationToken))
                : Results.Ok(await foleoTraderOrderService.Get(valuationDate, cancellationToken));
        });

        foleoTrader.MapPost("/Orders", async (FoleoTraderOrderRequest request, FoleoTraderOrderProcessor processor, FoleoTraderFixClient fixClient, CancellationToken cancellationToken) =>
        {
            var result = await processor.SubmitOrderAsync(request, fixClient, cancellationToken);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            return Results.Accepted("/API/Trading/FoleoTrader/Orders", new
            {
                EventID = result.Value.EventID.Value,
                result.Value.ClOrdID
            });
        });
    }

    private static void MapInstrumentEndpoints(this RouteGroupBuilder api)
    {
        var instruments = api.MapGroup("/Instruments").WithTags("Instruments");

        instruments.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, InstrumentService instrumentService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await instrumentService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await instrumentService.Get(valuationDate));
        });
    }

    private static void MapInstrumentValueEndpoints(this RouteGroupBuilder api)
    {
        var instrumentValues = api.MapGroup("/InstrumentValues").WithTags("Instrument Values");

        instrumentValues.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, InstrumentValueService instrumentValueService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await instrumentValueService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await instrumentValueService.Get(valuationDate));
        });
    }

    private static void MapTicketEndpoints(this RouteGroupBuilder api)
    {
        var tickets = api.MapGroup("/Tickets").WithTags("Tickets");

        tickets.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, bool? includeClosed, TicketService ticketService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var aggregate = auditDateTime.HasValue
                ? await ticketService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value))
                : await ticketService.Get(valuationDate);

            var items = includeClosed == true
                ? aggregate.Items
                : aggregate.Items.Where(ticket => ticket.IsActive).ToList();

            return Results.Ok(aggregate with { Items = items });
        });

        tickets.MapGet("/Details", async (DateTime eventDateTime, DateTime? auditDateTime, bool? includeClosed, TicketService ticketService, InstrumentService instrumentService, AccountService accountService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var auditDate = auditDateTime.HasValue ? AuditDateTimeBuilder.Create(auditDateTime.Value) : null;

            var ticketAggregate = auditDate is null
                ? await ticketService.Get(valuationDate)
                : await ticketService.Get(valuationDate, auditDate);
            var instruments = auditDate is null
                ? await instrumentService.Get(valuationDate)
                : await instrumentService.Get(valuationDate, auditDate);
            var accounts = auditDate is null
                ? await accountService.Get(valuationDate)
                : await accountService.Get(valuationDate, auditDate);

            return Results.Ok(new TicketDetails(ticketAggregate, instruments, accounts, includeClosed == true));
        });

        tickets.MapGet("/Stages", () =>
            Results.Ok(Enum.GetValues<TicketStage>()
                .Select(stage => new
                {
                    Stage = stage.ToString(),
                    Description = GetTicketStageDescription(stage)
                })
                .ToList()));

        tickets.MapGet("/{ticketNumber:int}", async (int ticketNumber, DateTime eventDateTime, DateTime? auditDateTime, TicketService ticketService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var aggregate = auditDateTime.HasValue
                ? await ticketService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value))
                : await ticketService.Get(valuationDate);
            var ticket = aggregate.Find(new TicketNumber(ticketNumber));

            return ticket is null
                ? Results.NotFound()
                : Results.Ok(ticket);
        });
    }

    private static string GetTicketStageDescription(TicketStage stage)
    {
        var member = typeof(TicketStage).GetMember(stage.ToString()).SingleOrDefault();
        return member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? stage.ToString();
    }

    private static void MapUserEndpoints(this RouteGroupBuilder api)
    {
        var users = api.MapGroup("/Users").WithTags("Users");

        var getUsers = async (DateTime eventDateTime, DateTime? auditDateTime, UserService userService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await userService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await userService.Get(valuationDate));
        };

        users.MapGet("", getUsers).ExcludeFromDescription();
        users.MapGet("/", getUsers);

        users.MapGet("/{userID:guid}", async (Guid userID, DateTime eventDateTime, DateTime? auditDateTime, UserService userService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var aggregate = auditDateTime.HasValue
                ? await userService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value))
                : await userService.Get(valuationDate);
            var user = aggregate.Find(new UserID(userID));

            return user is null
                ? Results.NotFound()
                : Results.Ok(user);
        });
    }

    private static void MapUserMenuPreferencesEndpoints(this RouteGroupBuilder api)
    {
        var userMenuPreferences = api.MapGroup("/UserMenuPreferences").WithTags("User Menu Preferences");

        userMenuPreferences.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, Guid? userID, UserMenuPreferencesService userMenuPreferencesService) =>
        {
            var resolvedUserID = new UserID(userID ?? Constants.Initialisation.UserID.Value);
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await userMenuPreferencesService.Get(resolvedUserID, valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await userMenuPreferencesService.Get(resolvedUserID, valuationDate));
        });
    }

    private static void MapUserValuationPreferencesEndpoints(this RouteGroupBuilder api)
    {
        var userValuationPreferences = api.MapGroup("/UserValuationPreferences").WithTags("User Valuation Preferences");

        userValuationPreferences.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, Guid? userID, UserValuationPreferencesService userValuationPreferencesService) =>
        {
            var resolvedUserID = new UserID(userID ?? Constants.Initialisation.UserID.Value);
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await userValuationPreferencesService.Get(resolvedUserID, valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await userValuationPreferencesService.Get(resolvedUserID, valuationDate));
        });
    }

    private static void MapUserBookmarksEndpoints(this RouteGroupBuilder api)
    {
        var userBookmarks = api.MapGroup("/UserBookmarks").WithTags("User Bookmarks");

        userBookmarks.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, Guid? userID, UserBookmarksService userBookmarksService) =>
        {
            var resolvedUserID = new UserID(userID ?? Constants.Initialisation.UserID.Value);
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await userBookmarksService.Get(resolvedUserID, valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await userBookmarksService.Get(resolvedUserID, valuationDate));
        });
    }

    private static void MapAccountEventEndpoints(this RouteGroupBuilder api)
    {
        var accountEvents = api.MapGroup("/Events/Account").WithTags("Account Events");

        accountEvents.MapGet("/", async (Guid? accountID, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IAccountEvent>(Constants.Initialisation.AccountsStreamId, cancellationToken);
            if (accountID.HasValue)
                events = events.Where(@event => GetAccountID(@event) == accountID.Value).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToAccountEventResponse));
        });

        accountEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IAccountEvent accountEvent
                ? Results.Ok(ToAccountEventResponse(accountEvent))
                : Results.NotFound();
        });

        accountEvents.MapPost($"/{nameof(AccountCreatedEvent)}", async (IEventRepository eventRepository, CurrencyService currencyService, AggregateCacheInvalidationService cacheInvalidationService, AccountCreatedRequest request, CancellationToken cancellationToken) =>
        {
            var asAt = AuditDateTimeBuilder.Create();
            var currencies = await currencyService.Get(request.EventDateTime, asAt);
            var accounts = await TryGetAccounts(request.EventDateTime, asAt, eventRepository, cancellationToken);
            var result = AccountCreatedEventBuilder.Create(request, currencies, accounts);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            await eventRepository.AppendAsync(Constants.Initialisation.AccountsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Accepted(AccountEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(AccountEventsRoute, result.Value));
        });

        accountEvents.MapPost($"/{nameof(AccountModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AccountModifiedRequest request, CancellationToken cancellationToken) =>
        {
            var asAt = AuditDateTimeBuilder.Create();
            var accounts = await TryGetAccounts(request.EventDateTime, asAt, eventRepository, cancellationToken);
            if (accounts is null)
                return Results.BadRequest(Result<AccountModifiedEvent>.Invalid([$"No matching Account found for AccountID '{request.AccountID}'."]));

            var result = AccountModifiedEventBuilder.Create(request, accounts);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            await eventRepository.AppendAsync(Constants.Initialisation.AccountsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Accepted(AccountEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(AccountEventsRoute, result.Value));
        });

        accountEvents.MapPost($"/{nameof(AccountActiveSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AccountActiveSetRequest request, CancellationToken cancellationToken) =>
        {
            var accounts = await TryGetAccounts(request.EventDateTime, AuditDateTimeBuilder.Create(), eventRepository, cancellationToken);
            if (accounts is null)
                return Results.BadRequest(Result<AccountActiveSetEvent>.Invalid([$"No matching Account found for AccountID '{request.AccountID}'."]));

            var result = AccountActiveSetEventBuilder.Create(request, accounts);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            await eventRepository.AppendAsync(Constants.Initialisation.AccountsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Accepted(AccountEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(AccountEventsRoute, result.Value));
        });

        accountEvents.MapPost($"/{nameof(AccountDisplayOrderSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AccountDisplayOrderSetRequest request, CancellationToken cancellationToken) =>
        {
            var accounts = await TryGetAccounts(request.EventDateTime, AuditDateTimeBuilder.Create(), eventRepository, cancellationToken);
            if (accounts is null)
                return Results.BadRequest(Result<AccountDisplayOrderSetEvent>.Invalid([$"No matching Account found for AccountID '{request.AccountID}'."]));

            var result = AccountDisplayOrderSetEventBuilder.Create(request, accounts);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            await eventRepository.AppendAsync(Constants.Initialisation.AccountsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Accepted(AccountEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(AccountEventsRoute, result.Value));
        });
    }

    private static void MapBrokerEventEndpoints(this RouteGroupBuilder api)
    {
        var brokerEvents = api.MapGroup("/Events/Broker").WithTags("Broker Events");

        brokerEvents.MapGet("/", async (string? lei, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IBrokerEvent>(Constants.Initialisation.BrokersStreamId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(lei))
                events = events.Where(@event => string.Equals(GetBrokerLEI(@event), lei, StringComparison.OrdinalIgnoreCase)).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToBrokerEventResponse));
        });

        brokerEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IBrokerEvent brokerEvent
                ? Results.Ok(ToBrokerEventResponse(brokerEvent))
                : Results.NotFound();
        });

        brokerEvents.MapPost($"/{nameof(BrokerCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, BrokerCreatedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.BrokersStreamId,
                BrokerEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => BrokerCreatedEventBuilder.Create(request),
                cancellationToken));

        brokerEvents.MapPost($"/{nameof(BrokerModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, BrokerModifiedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.BrokersStreamId,
                BrokerEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => BrokerModifiedEventBuilder.Create(request),
                cancellationToken));

        brokerEvents.MapPost($"/{nameof(BrokerActiveSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, BrokerActiveSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.BrokersStreamId,
                BrokerEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => BrokerActiveSetEventBuilder.Create(request),
                cancellationToken));

        brokerEvents.MapPost($"/{nameof(BrokerApprovedDateTimeSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, BrokerApprovedDateTimeSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.BrokersStreamId,
                BrokerEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => BrokerApprovedDateTimeSetEventBuilder.Create(request),
                cancellationToken));

        brokerEvents.MapPost($"/{nameof(BrokerNextReviewSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, BrokerNextReviewSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.BrokersStreamId,
                BrokerEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => BrokerNextReviewSetEventBuilder.Create(request),
                cancellationToken));

        brokerEvents.MapPost($"/{nameof(BrokerNotesSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, BrokerNotesSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.BrokersStreamId,
                BrokerEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => BrokerNotesSetEventBuilder.Create(request),
                cancellationToken));
    }

    private static void MapCountryEventEndpoints(this RouteGroupBuilder api)
    {
        var countryEvents = api.MapGroup("/Events/Country").WithTags("Country Events");

        countryEvents.MapGet("/", async (string? alpha2, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<ICountryEvent>(Constants.Initialisation.CountriesStreamId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(alpha2))
                events = events.Where(@event => string.Equals(GetCountryAlpha2(@event), alpha2, StringComparison.OrdinalIgnoreCase)).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToCountryEventResponse));
        });

        countryEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is ICountryEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        countryEvents.MapPost($"/{nameof(CountryCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CountryCreatedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.CountriesStreamId,
                CountryEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => CountryCreatedEventBuilder.Create(request),
                cancellationToken));

        countryEvents.MapPost($"/{nameof(CountryModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CountryModifiedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.CountriesStreamId,
                CountryEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => CountryModifiedEventBuilder.Create(request),
                cancellationToken));

        countryEvents.MapPost($"/{nameof(CountryFlagModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CountryFlagModifiedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.CountriesStreamId,
                CountryEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => CountryFlagModifiedEventBuilder.Create(request),
                cancellationToken));
    }

    private static void MapCurrencyEventEndpoints(this RouteGroupBuilder api)
    {
        var currencyEvents = api.MapGroup("/Events/Currency").WithTags("Currency Events");

        currencyEvents.MapGet("/", async (string? alphabeticCode, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<ICurrencyEvent>(Constants.Initialisation.CurrenciesStreamId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(alphabeticCode))
                events = events.Where(@event => string.Equals(GetCurrencyAlphabeticCode(@event), alphabeticCode, StringComparison.OrdinalIgnoreCase)).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToCurrencyEventResponse));
        });

        currencyEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is ICurrencyEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        currencyEvents.MapPost($"/{nameof(CurrencyCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CurrencyCreatedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.CurrenciesStreamId,
                CurrencyEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => CurrencyCreatedEventBuilder.Create(request),
                cancellationToken));

        currencyEvents.MapPost($"/{nameof(CurrencyModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CurrencyModifiedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.CurrenciesStreamId,
                CurrencyEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => CurrencyModifiedEventBuilder.Create(request),
                cancellationToken));
    }

    private static void MapValuationSettingEventEndpoints(this RouteGroupBuilder api)
    {
        var valuationSettingEvents = api.MapGroup("/Events/ValuationSetting").WithTags("Valuation Setting Events");

        valuationSettingEvents.MapGet("/", async (Guid? assetAllocationID, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IValuationSettingEvent>(Constants.Initialisation.ValuationSettingsStreamId, cancellationToken);
            if (assetAllocationID.HasValue)
                events = events.Where(@event => GetAssetAllocationID(@event) == assetAllocationID.Value).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToValuationSettingEventResponse));
        });

        valuationSettingEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IAuditEventBase>(eventId, cancellationToken);
            return @event is IValuationSettingEvent valuationSettingEvent
                ? Results.Ok(ToValuationSettingEventResponse(valuationSettingEvent))
                : Results.NotFound();
        });

        valuationSettingEvents.MapPost($"/{nameof(AssetAllocationCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AssetAllocationCreatedRequest request, CancellationToken cancellationToken) =>
        {
            var valuationSettings = await TryGetValuationSettings(Constants.Valuation.Today, AuditDateTimeBuilder.Create(), eventRepository, cancellationToken);
            return await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.ValuationSettingsStreamId,
                ValuationSettingEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => AssetAllocationCreatedEventBuilder.Create(request, valuationSettings),
                cancellationToken);
        });

        valuationSettingEvents.MapPost($"/{nameof(AssetAllocationModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AssetAllocationModifiedRequest request, CancellationToken cancellationToken) =>
        {
            var valuationSettings = await TryGetValuationSettings(Constants.Valuation.Today, AuditDateTimeBuilder.Create(), eventRepository, cancellationToken);
            return await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.ValuationSettingsStreamId,
                ValuationSettingEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => AssetAllocationModifiedEventBuilder.Create(request, valuationSettings),
                cancellationToken);
        });

        valuationSettingEvents.MapPost($"/{nameof(AssetAllocationAccountIDsSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AssetAllocationAccountIDsSetRequest request, CancellationToken cancellationToken) =>
        {
            var valuationSettings = await TryGetValuationSettings(Constants.Valuation.Today, AuditDateTimeBuilder.Create(), eventRepository, cancellationToken);
            return await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.ValuationSettingsStreamId,
                ValuationSettingEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => AssetAllocationAccountIDsSetEventBuilder.Create(request, valuationSettings),
                cancellationToken);
        });

        valuationSettingEvents.MapPost($"/{nameof(AssetAllocationActiveSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AssetAllocationActiveSetRequest request, CancellationToken cancellationToken) =>
        {
            var valuationSettings = await TryGetValuationSettings(Constants.Valuation.Today, AuditDateTimeBuilder.Create(), eventRepository, cancellationToken);
            return await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.ValuationSettingsStreamId,
                ValuationSettingEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => AssetAllocationActiveSetEventBuilder.Create(request, valuationSettings),
                cancellationToken);
        });
    }

    private static void MapAssetAllocationMappingEventEndpoints(this RouteGroupBuilder api)
    {
        var mappingEvents = api.MapGroup("/Events/AssetAllocationMapping").WithTags("Asset Allocation Mapping Events");

        mappingEvents.MapGet("/", async (Guid? assetAllocationID, Guid? holdingID, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IAssetAllocationMappingEvent>(Constants.Initialisation.AssetAllocationMappingsStreamId, cancellationToken);

            if (assetAllocationID.HasValue)
                events = events.Where(@event => @event.AssetAllocationID.Value == assetAllocationID.Value).ToList();

            if (holdingID.HasValue)
                events = events.Where(@event => @event.HoldingID.Value == holdingID.Value).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToAssetAllocationMappingEventResponse));
        });

        mappingEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IAuditEventBase>(eventId, cancellationToken);
            return @event is IAssetAllocationMappingEvent mappingEvent
                ? Results.Ok(ToAssetAllocationMappingEventResponse(mappingEvent))
                : Results.NotFound();
        });

        mappingEvents.MapPost($"/{nameof(AssetAllocationMappingSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AssetAllocationMappingRequest request, CancellationToken cancellationToken) =>
        {
            var asAt = AuditDateTimeBuilder.Create();
            var valuationSettings = await TryGetValuationSettings(request.EventDateTime, asAt, eventRepository, cancellationToken);
            var holdings = await TryGetHoldings(request.EventDateTime, asAt, eventRepository, cancellationToken);
            return await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.AssetAllocationMappingsStreamId,
                AssetAllocationMappingEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => AssetAllocationMappingEventBuilder.Create(request, valuationSettings, holdings),
                cancellationToken);
        });
    }

    private static void MapReportEventEndpoints(this RouteGroupBuilder api)
    {
        var reportEvents = api.MapGroup("/Events/Report").WithTags("Report Events");

        reportEvents.MapGet("/", async (Guid? reportID, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IReportEvent>(Constants.Initialisation.ReportConfigsStreamId, cancellationToken);
            if (reportID.HasValue)
                events = events.Where(@event => @event.ReportID.Value == reportID.Value).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToReportEventResponse));
        });

        reportEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IAuditEventBase>(eventId, cancellationToken);
            return @event is IReportEvent reportEvent
                ? Results.Ok(ToReportEventResponse(reportEvent))
                : Results.NotFound();
        });

        reportEvents.MapPost($"/{nameof(ReportCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, ReportCreatedRequest request, CancellationToken cancellationToken) =>
        {
            var asAt = AuditDateTimeBuilder.Create();
            var reports = await TryGetReportConfigs(Constants.Valuation.Today, asAt, eventRepository, cancellationToken);
            var valuationSettings = await TryGetValuationSettings(Constants.Valuation.Today, asAt, eventRepository, cancellationToken);
            return await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.ReportConfigsStreamId,
                ReportEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => ReportCreatedEventBuilder.Create(request, reports, valuationSettings),
                cancellationToken);
        });

        reportEvents.MapPost($"/{nameof(ReportModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, ReportModifiedRequest request, CancellationToken cancellationToken) =>
        {
            var asAt = AuditDateTimeBuilder.Create();
            var reports = await TryGetReportConfigs(Constants.Valuation.Today, asAt, eventRepository, cancellationToken);
            var valuationSettings = await TryGetValuationSettings(Constants.Valuation.Today, asAt, eventRepository, cancellationToken);
            return await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.ReportConfigsStreamId,
                ReportEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => ReportModifiedEventBuilder.Create(request, reports, valuationSettings),
                cancellationToken);
        });
    }

    private static void MapFXEventEndpoints(this RouteGroupBuilder api)
    {
        var fxEvents = api.MapGroup("/Events/FX").WithTags("FX Events");

        fxEvents.MapGet("/", async (string? pair, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(pair))
                events = events.Where(@event => string.Equals(GetFXPair(@event), pair, StringComparison.OrdinalIgnoreCase)).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToFXEventResponse));
        });

        fxEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IFXEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        fxEvents.MapPost($"/{nameof(FXCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, IFXRateReadModelRepository fxRateReadModelRepository, FXCreatedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.FXsStreamId,
                FXEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => FXCreatedEventBuilder.Create(request),
                cancellationToken,
                (_, token) => fxRateReadModelRepository.ClearAsync(token)));

        fxEvents.MapPost($"/{nameof(FXActiveModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, IFXRateReadModelRepository fxRateReadModelRepository, FXActiveModifiedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.FXsStreamId,
                FXEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => FXActiveModifiedEventBuilder.Create(request),
                cancellationToken,
                (_, token) => fxRateReadModelRepository.ClearAsync(token)));
    }

    private static void MapFXRateEventEndpoints(this RouteGroupBuilder api)
    {
        var fxRateEvents = api.MapGroup("/Events/FXRate").WithTags("FX Rate Events");

        fxRateEvents.MapGet("/", async (string? pair, DateTime? rateValuationDateTime, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IFXRateEvent>(Constants.Initialisation.FXRatesStreamId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(pair))
                events = events.Where(@event => string.Equals(GetFXRatePair(@event), pair, StringComparison.OrdinalIgnoreCase)).ToList();

            if (rateValuationDateTime.HasValue)
                events = events.Where(@event => @event.EventDateTime.Value == rateValuationDateTime.Value).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToFXRateEventResponse));
        });

        fxRateEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IFXRateEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        fxRateEvents.MapPost($"/{nameof(FXRateSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, IFXRateReadModelRepository fxRateReadModelRepository, FXRateSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.FXRatesStreamId,
                FXRateEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => FXRateSetEventBuilder.Create(request),
                cancellationToken,
                (_, token) => fxRateReadModelRepository.ClearAsync(token)));
    }

    private static void MapHoldingEventEndpoints(this RouteGroupBuilder api)
    {
        var holdingEvents = api.MapGroup("/Events/Holding").WithTags("Holding Events");

        holdingEvents.MapGet("/", async (Guid? holdingID, Guid? accountID, Guid? instrumentID, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IHoldingEvent>(Constants.Initialisation.HoldingsStreamId, cancellationToken);

            if (holdingID.HasValue)
                events = events
                    .Where(@event => @event.HoldingID.Value == holdingID.Value)
                    .ToList();

            if (accountID.HasValue)
                events = events
                    .Where(@event => @event is HoldingCreatedEvent createdEvent && createdEvent.AccountID.Value == accountID.Value)
                    .ToList();

            if (instrumentID.HasValue)
                events = events
                    .Where(@event => @event is HoldingCreatedEvent createdEvent && createdEvent.InstrumentID.Value == instrumentID.Value)
                    .ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToHoldingEventResponse));
        });

        holdingEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IHoldingEvent holdingEvent
                ? Results.Ok(ToHoldingEventResponse(holdingEvent))
                : Results.NotFound();
        });

        MapHoldingCreatedEndpoint<HoldingPositionMemoCreatedRequest, HoldingPositionMemoCreatedEvent>(holdingEvents, nameof(HoldingPositionMemoCreatedEvent), HoldingPositionMemoCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingPositionCashCreatedRequest, HoldingPositionCashCreatedEvent>(holdingEvents, nameof(HoldingPositionCashCreatedEvent), HoldingPositionCashCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingPositionAssetCreatedRequest, HoldingPositionAssetCreatedEvent>(holdingEvents, nameof(HoldingPositionAssetCreatedEvent), HoldingPositionAssetCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingCashDebtCreatedRequest, HoldingCashDebtCreatedEvent>(holdingEvents, nameof(HoldingCashDebtCreatedEvent), HoldingCashDebtCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingCashInvestableCreatedRequest, HoldingCashInvestableCreatedEvent>(holdingEvents, nameof(HoldingCashInvestableCreatedEvent), HoldingCashInvestableCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingCashNonInvestableCreatedRequest, HoldingCashNonInvestableCreatedEvent>(holdingEvents, nameof(HoldingCashNonInvestableCreatedEvent), HoldingCashNonInvestableCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingNominalInflowCreatedRequest, HoldingNominalInflowCreatedEvent>(holdingEvents, nameof(HoldingNominalInflowCreatedEvent), HoldingNominalInflowCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingNominalOutflowCreatedRequest, HoldingNominalOutflowCreatedEvent>(holdingEvents, nameof(HoldingNominalOutflowCreatedEvent), HoldingNominalOutflowCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingNominalInSpecieInCreatedRequest, HoldingNominalInSpecieInCreatedEvent>(holdingEvents, nameof(HoldingNominalInSpecieInCreatedEvent), HoldingNominalInSpecieInCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingNominalInSpecieOutCreatedRequest, HoldingNominalInSpecieOutCreatedEvent>(holdingEvents, nameof(HoldingNominalInSpecieOutCreatedEvent), HoldingNominalInSpecieOutCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingNominalFeesCustodianCreatedRequest, HoldingNominalFeesCustodianCreatedEvent>(holdingEvents, nameof(HoldingNominalFeesCustodianCreatedEvent), HoldingNominalFeesCustodianCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingNominalFeesAdministratorCreatedRequest, HoldingNominalFeesAdministratorCreatedEvent>(holdingEvents, nameof(HoldingNominalFeesAdministratorCreatedEvent), HoldingNominalFeesAdministratorCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingNominalFeesBankCreatedRequest, HoldingNominalFeesBankCreatedEvent>(holdingEvents, nameof(HoldingNominalFeesBankCreatedEvent), HoldingNominalFeesBankCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingNominalIncomeCreatedRequest, HoldingNominalIncomeCreatedEvent>(holdingEvents, nameof(HoldingNominalIncomeCreatedEvent), HoldingNominalIncomeCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingNominalInterestCreatedRequest, HoldingNominalInterestCreatedEvent>(holdingEvents, nameof(HoldingNominalInterestCreatedEvent), HoldingNominalInterestCreatedEventBuilder.Create);

        MapHoldingModifiedEndpoint<HoldingPositionMemoModifiedRequest, HoldingPositionMemoModifiedEvent>(holdingEvents, nameof(HoldingPositionMemoModifiedEvent), HoldingPositionMemoModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingPositionCashModifiedRequest, HoldingPositionCashModifiedEvent>(holdingEvents, nameof(HoldingPositionCashModifiedEvent), HoldingPositionCashModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingPositionAssetModifiedRequest, HoldingPositionAssetModifiedEvent>(holdingEvents, nameof(HoldingPositionAssetModifiedEvent), HoldingPositionAssetModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingCashDebtModifiedRequest, HoldingCashDebtModifiedEvent>(holdingEvents, nameof(HoldingCashDebtModifiedEvent), HoldingCashDebtModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingCashInvestableModifiedRequest, HoldingCashInvestableModifiedEvent>(holdingEvents, nameof(HoldingCashInvestableModifiedEvent), HoldingCashInvestableModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingCashNonInvestableModifiedRequest, HoldingCashNonInvestableModifiedEvent>(holdingEvents, nameof(HoldingCashNonInvestableModifiedEvent), HoldingCashNonInvestableModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingNominalInflowModifiedRequest, HoldingNominalInflowModifiedEvent>(holdingEvents, nameof(HoldingNominalInflowModifiedEvent), HoldingNominalInflowModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingNominalOutflowModifiedRequest, HoldingNominalOutflowModifiedEvent>(holdingEvents, nameof(HoldingNominalOutflowModifiedEvent), HoldingNominalOutflowModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingNominalInSpecieInModifiedRequest, HoldingNominalInSpecieInModifiedEvent>(holdingEvents, nameof(HoldingNominalInSpecieInModifiedEvent), HoldingNominalInSpecieInModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingNominalInSpecieOutModifiedRequest, HoldingNominalInSpecieOutModifiedEvent>(holdingEvents, nameof(HoldingNominalInSpecieOutModifiedEvent), HoldingNominalInSpecieOutModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingNominalFeesCustodianModifiedRequest, HoldingNominalFeesCustodianModifiedEvent>(holdingEvents, nameof(HoldingNominalFeesCustodianModifiedEvent), HoldingNominalFeesCustodianModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingNominalFeesAdministratorModifiedRequest, HoldingNominalFeesAdministratorModifiedEvent>(holdingEvents, nameof(HoldingNominalFeesAdministratorModifiedEvent), HoldingNominalFeesAdministratorModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingNominalFeesBankModifiedRequest, HoldingNominalFeesBankModifiedEvent>(holdingEvents, nameof(HoldingNominalFeesBankModifiedEvent), HoldingNominalFeesBankModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingNominalIncomeModifiedRequest, HoldingNominalIncomeModifiedEvent>(holdingEvents, nameof(HoldingNominalIncomeModifiedEvent), HoldingNominalIncomeModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingNominalInterestModifiedRequest, HoldingNominalInterestModifiedEvent>(holdingEvents, nameof(HoldingNominalInterestModifiedEvent), HoldingNominalInterestModifiedEventBuilder.Create);

        holdingEvents.MapPost($"/{nameof(HoldingActiveModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, HoldingActiveModifiedRequest request, CancellationToken cancellationToken) =>
        {
            var holdings = await TryGetHoldings(request.EventDateTime, AuditDateTimeBuilder.Create(), eventRepository, cancellationToken);
            if (holdings is null)
                return Results.BadRequest(Result<HoldingActiveModifiedEvent>.Invalid([$"No matching Holding found for HoldingID '{request.HoldingID}'."]));

            var result = HoldingActiveModifiedEventBuilder.Create(request, holdings);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            await eventRepository.AppendAsync(Constants.Initialisation.HoldingsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Accepted(HoldingEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(HoldingEventsRoute, result.Value));
        });
    }

    private static void MapHoldingCreatedEndpoint<TRequest, TEvent>(
        RouteGroupBuilder holdingEvents,
        string eventName,
        Func<TRequest, Accounts?, Instruments?, Holdings?, Result<TEvent>> createEvent)
        where TRequest : IHoldingCreatedRequest
        where TEvent : HoldingCreatedEvent
    {
        holdingEvents.MapPost($"/{eventName}", async (IEventRepository eventRepository, AccountService accountService, InstrumentService instrumentService, AggregateCacheInvalidationService cacheInvalidationService, TRequest request, CancellationToken cancellationToken) =>
        {
            var asAt = AuditDateTimeBuilder.Create();
            var accounts = await accountService.Get(request.EventDateTime, asAt);
            var instruments = await instrumentService.Get(request.EventDateTime, asAt);
            var holdings = await TryGetHoldings(request.EventDateTime, asAt, eventRepository, cancellationToken);
            var result = createEvent(request, accounts, instruments, holdings);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            await eventRepository.AppendAsync(Constants.Initialisation.HoldingsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Accepted(HoldingEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(HoldingEventsRoute, result.Value));
        });
    }

    private static void MapHoldingModifiedEndpoint<TRequest, TEvent>(
        RouteGroupBuilder holdingEvents,
        string eventName,
        Func<TRequest, Holdings?, Result<TEvent>> createEvent)
        where TRequest : IHoldingModifiedRequest
        where TEvent : HoldingModifiedEvent
    {
        holdingEvents.MapPost($"/{eventName}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TRequest request, CancellationToken cancellationToken) =>
        {
            var holdings = await TryGetHoldings(request.EventDateTime, AuditDateTimeBuilder.Create(), eventRepository, cancellationToken);
            if (holdings is null)
                return Results.BadRequest(Result<TEvent>.Invalid([$"No matching Holding found for HoldingID '{request.HoldingID}'."]));

            var result = createEvent(request, holdings);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            await eventRepository.AppendAsync(Constants.Initialisation.HoldingsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Accepted(HoldingEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(HoldingEventsRoute, result.Value));
        });
    }

    private static void MapInstrumentEventEndpoints(this RouteGroupBuilder api)
    {
        var instrumentEvents = api.MapGroup("/Events/Instrument").WithTags("Instrument Events");

        instrumentEvents.MapGet("/", async (Guid? instrumentID, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId, cancellationToken);
            if (instrumentID.HasValue)
                events = events.Where(@event => GetInstrumentID(@event) == instrumentID.Value).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToInstrumentEventResponse));
        });

        instrumentEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IInstrumentEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        instrumentEvents.MapPost($"/{nameof(InstrumentCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, InstrumentCreatedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.InstrumentsStreamId,
                InstrumentEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => InstrumentCreatedEventBuilder.Create(request),
                cancellationToken));

        instrumentEvents.MapPost($"/{nameof(InstrumentModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, InstrumentModifiedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.InstrumentsStreamId,
                InstrumentEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => InstrumentModifiedEventBuilder.Create(request),
                cancellationToken));

        instrumentEvents.MapPost($"/{nameof(InstrumentActiveModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, InstrumentActiveModifiedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.InstrumentsStreamId,
                InstrumentEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => InstrumentActiveModifiedEventBuilder.Create(request),
                cancellationToken));

        instrumentEvents.MapPost($"/{nameof(InstrumentIdentifierSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, InstrumentIdentifierSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.InstrumentsStreamId,
                InstrumentEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => InstrumentIdentifierSetEventBuilder.Create(request),
                cancellationToken));

        instrumentEvents.MapPost($"/{nameof(InstrumentIdentifierUnsetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, InstrumentIdentifierUnsetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.InstrumentsStreamId,
                InstrumentEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => InstrumentIdentifierUnsetEventBuilder.Create(request),
                cancellationToken));

        instrumentEvents.MapPost($"/{nameof(InstrumentTermsSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, InstrumentTermsSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.InstrumentsStreamId,
                InstrumentEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => InstrumentTermsSetEventBuilder.Create(request),
                cancellationToken));
    }

    private static void MapInstrumentPriceEventEndpoints(this RouteGroupBuilder api)
    {
        var priceEvents = api.MapGroup("/Events/InstrumentPrice").WithTags("Instrument Price Events");

        priceEvents.MapGet("/", async (Guid? instrumentID, DateTime? priceValuationDateTime, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IInstrumentPriceEvent>(Constants.Initialisation.InstrumentPricesStreamId, cancellationToken);
            if (instrumentID.HasValue)
                events = events
                    .Where(@event => @event is InstrumentPriceSetEvent setEvent && setEvent.InstrumentID.Value == instrumentID.Value)
                    .ToList();

            if (priceValuationDateTime.HasValue)
                events = events.Where(@event => @event.EventDateTime.Value == priceValuationDateTime.Value).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToInstrumentPriceEventResponse));
        });

        priceEvents.MapPost($"/{nameof(InstrumentPriceSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, InstrumentPriceSetRequest request, CancellationToken cancellationToken) =>
        {
            var result = InstrumentPriceSetEventBuilder.Create(request);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            var validationErrors = await ValidateInstrumentValueEvent(result.Value.InstrumentID, result.Value.EventDateTime, result.Value.AuditDateTime, eventRepository, cancellationToken);
            if (validationErrors.Count > 0)
                return Results.BadRequest(Result<InstrumentPriceSetEvent>.Invalid(validationErrors));

            await eventRepository.AppendAsync(Constants.Initialisation.InstrumentPricesStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Accepted(InstrumentPriceEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(InstrumentPriceEventsRoute, result.Value));
        });
    }

    private static void MapInstrumentIncomeEventEndpoints(this RouteGroupBuilder api)
    {
        var incomeEvents = api.MapGroup("/Events/InstrumentIncome").WithTags("Instrument Income Events");

        incomeEvents.MapGet("/", async (Guid? instrumentID, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IInstrumentIncomeEvent>(Constants.Initialisation.InstrumentIncomesStreamId, cancellationToken);
            if (instrumentID.HasValue)
                events = events
                    .Where(@event => @event is InstrumentIncomeSetEvent setEvent && setEvent.InstrumentID.Value == instrumentID.Value)
                    .ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToInstrumentIncomeEventResponse));
        });

        incomeEvents.MapPost($"/{nameof(InstrumentIncomeSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, InstrumentIncomeSetRequest request, CancellationToken cancellationToken) =>
        {
            var result = InstrumentIncomeSetEventBuilder.Create(request);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            var validationErrors = await ValidateInstrumentValueEvent(result.Value.InstrumentID, result.Value.EventDateTime, result.Value.AuditDateTime, eventRepository, cancellationToken);
            if (validationErrors.Count > 0)
                return Results.BadRequest(Result<InstrumentIncomeSetEvent>.Invalid(validationErrors));

            await eventRepository.AppendAsync(Constants.Initialisation.InstrumentIncomesStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Accepted(InstrumentIncomeEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(InstrumentIncomeEventsRoute, result.Value));
        });
    }

    private static void MapTransactionEventEndpoints(this RouteGroupBuilder api)
    {
        var transactionEvents = api.MapGroup("/Events/Transaction").WithTags("Transaction Events");

        transactionEvents.MapGet("/", async (Guid? eventSetID, Guid? holdingID, Guid? accountID, Guid? instrumentID, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId, cancellationToken);

            if (eventSetID.HasValue)
                events = events
                    .Where(@event => @event switch
                    {
                        ITransactionMovementEvent movementEvent => movementEvent.EventSetID.Value == eventSetID.Value,
                        TransactionCancellationEvent cancellationEvent => cancellationEvent.EventSetID.Value == eventSetID.Value,
                        TransactionBookCostAdjustedEvent adjustmentEvent => adjustmentEvent.EventSetID.Value == eventSetID.Value,
                        _ => false
                    })
                    .ToList();

            if (holdingID.HasValue)
            {
                var holdingMovementEventIds = events
                    .OfType<ITransactionMovementEvent>()
                    .Where(@event => @event.HoldingID?.Value == holdingID.Value)
                    .Select(@event => @event.EventID.Value)
                    .ToHashSet();
                var holdingMovementEventSetIds = events
                    .OfType<ITransactionMovementEvent>()
                    .Where(@event => @event.HoldingID?.Value == holdingID.Value)
                    .Select(@event => @event.EventSetID.Value)
                    .ToHashSet();

                events = events
                    .Where(@event => @event switch
                    {
                        ITransactionMovementEvent movementEvent => movementEvent.HoldingID?.Value == holdingID.Value,
                        TransactionCancellationEvent cancellationEvent => holdingMovementEventIds.Contains(cancellationEvent.CancelledEventID.Value) ||
                            cancellationEvent.CancelledIDGroup.Any(cancelled => holdingMovementEventIds.Contains(cancelled.Value)),
                        TransactionBookCostAdjustedEvent adjustmentEvent => holdingMovementEventSetIds.Contains(adjustmentEvent.EventSetID.Value),
                        _ => false
                    })
                    .ToList();
            }

            if (accountID.HasValue)
            {
                var accountMovementEventIds = events
                    .OfType<ITransactionMovementEvent>()
                    .Where(@event => @event.AccountID.Value == accountID.Value)
                    .Select(@event => @event.EventID.Value)
                    .ToHashSet();

                events = events
                    .Where(@event => @event switch
                    {
                        ITransactionMovementEvent movementEvent => movementEvent.AccountID.Value == accountID.Value,
                        TransactionCancellationEvent cancellationEvent => cancellationEvent.AccountID?.Value == accountID.Value ||
                            cancellationEvent.CancelledIDGroup.Any(cancelled => accountMovementEventIds.Contains(cancelled.Value)),
                        TransactionBookCostAdjustedEvent adjustmentEvent => adjustmentEvent.AccountID.Value == accountID.Value,
                        _ => false
                    })
                    .ToList();
            }

            if (instrumentID.HasValue)
            {
                var instrumentMovementEventSetIds = events
                    .OfType<ITransactionMovementEvent>()
                    .Where(@event => @event.InstrumentID.Value == instrumentID.Value)
                    .Select(@event => @event.EventSetID.Value)
                    .ToHashSet();

                events = events
                    .Where(@event => @event switch
                    {
                        ITransactionMovementEvent movementEvent => movementEvent.InstrumentID.Value == instrumentID.Value,
                        TransactionBookCostAdjustedEvent adjustmentEvent => instrumentMovementEventSetIds.Contains(adjustmentEvent.EventSetID.Value),
                        _ => false
                    })
                    .ToList();
            }

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToTransactionEventResponse));
        });

        transactionEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is ITransactionEvent transactionEvent
                ? Results.Ok(ToTransactionEventResponse(transactionEvent))
                : Results.NotFound();
        });

        transactionEvents.MapPost("/TransactionSet", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TransactionSetRequest request, CancellationToken cancellationToken) =>
        {
            var asAt = AuditDateTimeBuilder.Create();
            var accounts = await TryGetAccounts(request.EventDateTime, asAt, eventRepository, cancellationToken);
            var holdings = await TryGetHoldings(request.EventDateTime, asAt, eventRepository, cancellationToken);
            var result = TransactionBuilder.Create(request, holdings, accounts);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            var events = result.Value.Cast<IEventBase>().ToList();
            await eventRepository.AppendAsync(Constants.Initialisation.TransactionsStreamId, events, cancellationToken);
            cacheInvalidationService.Invalidate(events);

            return Results.Accepted(TransactionEventsRoute, CreateAcceptedEventsResponse(TransactionEventsRoute, events));
        });

        transactionEvents.MapPost("/BookCostAdjustment", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TransactionBookCostAdjustmentRequest request, CancellationToken cancellationToken) =>
        {
            var existingEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId, cancellationToken);
            var result = TransactionBookCostAdjustedEventBuilder.Create(request, existingEvents);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            await eventRepository.AppendAsync(Constants.Initialisation.TransactionsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);

            return Results.Accepted(TransactionEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(TransactionEventsRoute, result.Value));
        });

        transactionEvents.MapPost("/Cancel", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TransactionCancellationRequest request, CancellationToken cancellationToken) =>
        {
            var existingEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId, cancellationToken);

            Result<IReadOnlyList<TransactionCancellationEvent>> result;
            try
            {
                result = TransactionCancellationEventBuilder.Create(request, existingEvents);
            }
            catch (InvalidOperationException exception)
            {
                return Results.Conflict(Result<IReadOnlyList<TransactionCancellationEvent>>.Invalid([exception.Message]));
            }

            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            var events = result.Value.Cast<IEventBase>().ToList();
            await eventRepository.AppendAsync(Constants.Initialisation.TransactionsStreamId, events, cancellationToken);
            cacheInvalidationService.Invalidate(events);

            return Results.Accepted(TransactionEventsRoute, CreateAcceptedEventsResponse(TransactionEventsRoute, events));
        });
    }

    private static void MapTicketEventEndpoints(this RouteGroupBuilder api)
    {
        var ticketEvents = api.MapGroup("/Events/Ticket").WithTags("Ticket Events");

        ticketEvents.MapGet("/", async (int? ticketNumber, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<ITicket>(Constants.Initialisation.TicketsStreamId, cancellationToken);
            if (ticketNumber.HasValue)
                events = events.Where(@event => @event.TicketNumber.Value == ticketNumber.Value).ToList();

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToTicketEventResponse));
        });

        ticketEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is ITicket ticketEvent
                ? Results.Ok(ToTicketEventResponse(ticketEvent))
                : Results.NotFound();
        });

        ticketEvents.MapPost($"/{nameof(TicketCreatedEvent)}", async (IEventRepository eventRepository, InstrumentService instrumentService, AggregateCacheInvalidationService cacheInvalidationService, TicketCreatedRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var events = await eventRepository.LoadStreamAsync<ITicket>(Constants.Initialisation.TicketsStreamId, cancellationToken);
                    var instruments = await instrumentService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.Create(request, TicketEventBuilder.NextTicketNumber(events), instruments);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketAccountAddedEvent)}", async (TicketService ticketService, AccountService accountService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketAccountRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var tickets = await ticketService.Get(request.EventDateTime, asAt);
                    var accounts = await accountService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.AddAccount(request, tickets, accounts);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketAccountRemovedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketAccountRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.RemoveAccount(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalCreatedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketProposalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.CreateProposal(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalModifiedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketProposalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.ModifyProposal(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalDecisionRequestedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.RequestProposalDecision(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalApprovedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.ApproveProposal(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalNotApprovedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.NotApproveProposal(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalReasonSetEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTextSetRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.SetProposalReason(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketProposalAllocationSetEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTextSetRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.SetProposalAllocation(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeCreatedEvent)}", async (TicketService ticketService, HoldingService holdingService, InstrumentService instrumentService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var tickets = await ticketService.Get(request.EventDateTime, asAt);
                    var holdings = await holdingService.Get(request.EventDateTime, asAt);
                    var instruments = await instrumentService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.CreateTrade(request, tickets, holdings, instruments);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeModifiedEvent)}", async (TicketService ticketService, HoldingService holdingService, InstrumentService instrumentService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var tickets = await ticketService.Get(request.EventDateTime, asAt);
                    var holdings = await holdingService.Get(request.EventDateTime, asAt);
                    var instruments = await instrumentService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.ModifyTrade(request, tickets, holdings, instruments);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeFillAddedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeFillRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.AddFill(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeFillModifiedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeFillRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.ModifyFill(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeFillRemovedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeFillRemovedRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.RemoveFill(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeDecisionRequestedEvent)}", async (TicketService ticketService, HoldingService holdingService, InstrumentService instrumentService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var tickets = await ticketService.Get(request.EventDateTime, asAt);
                    var holdings = await holdingService.Get(request.EventDateTime, asAt);
                    var instruments = await instrumentService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.RequestTradeDecision(request, tickets, holdings, instruments);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeApprovedEvent)}", async (TicketService ticketService, AccountService accountService, InstrumentService instrumentService, FXRateService fxRateService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeApprovalRequest request, CancellationToken cancellationToken) =>
        {
            var asAt = AuditDateTimeBuilder.Create();
            var tickets = await ticketService.Get(request.EventDateTime, asAt);
            var accounts = await accountService.Get(request.EventDateTime, asAt);
            var instruments = await instrumentService.Get(request.EventDateTime, asAt);
            var ticket = tickets.Find(request.TicketNumber);
            var fxRates = ticket?.TradeDateTime is null
                ? null
                : await fxRateService.Get(ticket.TradeDateTime, asAt);
            var holdingEvents = await eventRepository.LoadStreamAsync<IHoldingEvent>(Constants.Initialisation.HoldingsStreamId, cancellationToken);
            var result = TicketEventBuilder.ApproveTradeWithTransactions(request, tickets, accounts, instruments, holdingEvents, fxRates);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            if (result.Value.HoldingEvents.Count > 0)
                await eventRepository.AppendAsync(Constants.Initialisation.HoldingsStreamId, result.Value.HoldingEvents.Cast<IEventBase>().ToList(), cancellationToken);
            await eventRepository.AppendAsync(Constants.Initialisation.TicketsStreamId, result.Value.ApprovalEvent, cancellationToken);
            var transactionEvents = result.Value.TransactionEvents.Cast<IEventBase>().ToList();
            await eventRepository.AppendAsync(Constants.Initialisation.TransactionsStreamId, transactionEvents, cancellationToken);

            cacheInvalidationService.Invalidate([.. result.Value.HoldingEvents, result.Value.ApprovalEvent, .. transactionEvents]);

            return Results.Accepted(TicketEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(TicketEventsRoute, result.Value.ApprovalEvent));
        });

        ticketEvents.MapPost($"/{nameof(TicketTradeNotApprovedEvent)}", async (TicketService ticketService, HoldingService holdingService, InstrumentService instrumentService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () =>
                {
                    var asAt = AuditDateTimeBuilder.Create();
                    var tickets = await ticketService.Get(request.EventDateTime, asAt);
                    var holdings = await holdingService.Get(request.EventDateTime, asAt);
                    var instruments = await instrumentService.Get(request.EventDateTime, asAt);
                    return TicketEventBuilder.NotApproveTrade(request, tickets, holdings, instruments);
                },
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeInstructionNotesSetEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTextSetRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.SetTradeInstructionNotes(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeProgressNotesSetEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTextSetRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.SetTradeProgressNotes(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketCancelledEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketCancellationRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.Cancel(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));
    }

    private static void MapUserEventEndpoints(this RouteGroupBuilder api)
    {
        var userEvents = api.MapGroup("/Events/User").WithTags("User Events");

        userEvents.MapGet("/", async (Guid? userID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IUserEvent>(Constants.Initialisation.UsersStreamId, cancellationToken);

            if (userID.HasValue)
                events = events.Where(@event => @event.UserID.Value == userID.Value).ToList();

            return Results.Ok(events.ToList());
        });

        userEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IUserEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        userEvents.MapPost($"/{nameof(UserCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserEventRequest request, CancellationToken cancellationToken) =>
        {
            var userResult = UserCreatedEventBuilder.Create(
                request.UserID,
                EventDateTimeBuilder.Create(request.EventDateTime),
                request.Reason,
                request.DisplayName,
                CreateUserDisplayPreferences(request),
                CreateUserValuationPreferences(request));

            if (!userResult.IsValid || userResult.Value is null)
                return Results.BadRequest(userResult);

            var menuPreferencesResult = UserMenuPreferencesCreatedEventBuilder.CreateDefault(userResult.Value);
            if (!menuPreferencesResult.IsValid || menuPreferencesResult.Value is null)
                return Results.BadRequest(menuPreferencesResult);

            var valuationPreferencesResult = UserValuationPreferencesCreatedEventBuilder.CreateDefault(userResult.Value);
            if (!valuationPreferencesResult.IsValid || valuationPreferencesResult.Value is null)
                return Results.BadRequest(valuationPreferencesResult);

            await eventRepository.AppendAsync(Constants.Initialisation.UsersStreamId, userResult.Value, cancellationToken);
            await eventRepository.AppendAsync(Constants.Initialisation.UserMenuPreferencesStreamId, menuPreferencesResult.Value, cancellationToken);
            await eventRepository.AppendAsync(Constants.Initialisation.UserValuationPreferencesStreamId, valuationPreferencesResult.Value, cancellationToken);

            cacheInvalidationService.Invalidate(userResult.Value);
            cacheInvalidationService.Invalidate(menuPreferencesResult.Value);
            cacheInvalidationService.Invalidate(valuationPreferencesResult.Value);

            return Results.Accepted(UserEventsRoute, CreateAcceptedEventsResponse([
                (UserEventsRoute, (IEventBase)userResult.Value),
                (UserMenuPreferencesEventsRoute, menuPreferencesResult.Value),
                (UserValuationPreferencesEventsRoute, valuationPreferencesResult.Value)
            ]));
        });

        userEvents.MapPost($"/{nameof(UserModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserEventRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UsersStreamId,
                UserEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserModifiedEventBuilder.Create(
                    request.UserID,
                    EventDateTimeBuilder.Create(request.EventDateTime),
                    request.Reason,
                    request.DisplayName,
                    CreateUserDisplayPreferences(request),
                    CreateUserValuationPreferences(request)),
                cancellationToken));

        userEvents.MapPost($"/{nameof(UserSignedInEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserSessionEventRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UsersStreamId,
                UserEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserSignedInEventBuilder.Create(
                    request.UserID,
                    EventDateTimeBuilder.Create(request.EventDateTime),
                    request.Reason),
                cancellationToken));

        userEvents.MapPost($"/{nameof(UserSignedOutEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserSessionEventRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UsersStreamId,
                UserEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserSignedOutEventBuilder.Create(
                    request.UserID,
                    EventDateTimeBuilder.Create(request.EventDateTime),
                    request.Reason),
                cancellationToken));
    }

    private static void MapUserMenuPreferencesEventEndpoints(this RouteGroupBuilder api)
    {
        var userMenuPreferencesEvents = api.MapGroup("/Events/UserMenuPreferences").WithTags("User Menu Preferences Events");

        userMenuPreferencesEvents.MapGet("/", async (Guid? userID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IUserMenuPreferencesEvent>(Constants.Initialisation.UserMenuPreferencesStreamId, cancellationToken);

            if (userID.HasValue)
                events = events.Where(@event => @event.UserID.Value == userID.Value).ToList();

            return Results.Ok(events.ToList());
        });

        userMenuPreferencesEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IUserMenuPreferencesEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        userMenuPreferencesEvents.MapPost($"/{nameof(UserMenuPreferencesCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserMenuPreferencesRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserMenuPreferencesStreamId,
                UserMenuPreferencesEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserMenuPreferencesCreatedEventBuilder.Create(request),
                cancellationToken));

        userMenuPreferencesEvents.MapPost($"/{nameof(UserMenuPreferencesModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserMenuPreferencesRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserMenuPreferencesStreamId,
                UserMenuPreferencesEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserMenuPreferencesModifiedEventBuilder.Create(request),
                cancellationToken));
    }

    private static void MapUserValuationPreferencesEventEndpoints(this RouteGroupBuilder api)
    {
        var userValuationPreferencesEvents = api.MapGroup("/Events/UserValuationPreferences").WithTags("User Valuation Preferences Events");

        userValuationPreferencesEvents.MapGet("/", async (Guid? userID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IUserValuationPreferencesEvent>(Constants.Initialisation.UserValuationPreferencesStreamId, cancellationToken);

            if (userID.HasValue)
                events = events.Where(@event => @event.UserID.Value == userID.Value).ToList();

            return Results.Ok(events.ToList());
        });

        userValuationPreferencesEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IUserValuationPreferencesEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        userValuationPreferencesEvents.MapPost($"/{nameof(UserValuationPreferencesCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserValuationPreferencesRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserValuationPreferencesStreamId,
                UserValuationPreferencesEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserValuationPreferencesCreatedEventBuilder.Create(request),
                cancellationToken));

        userValuationPreferencesEvents.MapPost($"/{nameof(UserValuationPreferencesModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserValuationPreferencesRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserValuationPreferencesStreamId,
                UserValuationPreferencesEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserValuationPreferencesModifiedEventBuilder.Create(request),
                cancellationToken));
    }

    private static void MapUserBookmarksEventEndpoints(this RouteGroupBuilder api)
    {
        var userBookmarksEvents = api.MapGroup("/Events/UserBookmarks").WithTags("User Bookmark Events");

        userBookmarksEvents.MapGet("/", async (Guid? userID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IUserBookmarksEvent>(Constants.Initialisation.UserBookmarksStreamId, cancellationToken);

            if (userID.HasValue)
                events = events.Where(@event => @event.UserID.Value == userID.Value).ToList();

            return Results.Ok(events.ToList());
        });

        userBookmarksEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IUserBookmarksEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        userBookmarksEvents.MapPost($"/{nameof(UserBookmarkCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserBookmarkRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserBookmarksStreamId,
                UserBookmarksEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserBookmarkCreatedEventBuilder.Create(request),
                cancellationToken));

        userBookmarksEvents.MapPost($"/{nameof(UserBookmarkModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserBookmarkRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserBookmarksStreamId,
                UserBookmarksEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserBookmarkModifiedEventBuilder.Create(request),
                cancellationToken));

        userBookmarksEvents.MapPost($"/{nameof(UserBookmarkDisplayOrderSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserBookmarkDisplayOrderSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserBookmarksStreamId,
                UserBookmarksEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserBookmarkDisplayOrderSetEventBuilder.Create(request),
                cancellationToken));

        userBookmarksEvents.MapPost($"/{nameof(UserBookmarkDeletedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserBookmarkDeletedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UserBookmarksStreamId,
                UserBookmarksEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserBookmarkDeletedEventBuilder.Create(request),
                cancellationToken));
    }

    private static UserDisplayPreferences CreateUserDisplayPreferences(UserEventRequest request) =>
        new(request.DisplayPreferences.DarkMode, request.DisplayPreferences.RememberTraceDate);

    private static UserProfileValuationPreferences CreateUserValuationPreferences(UserEventRequest request) =>
        new(
            EventDateTimeBuilder.Create(request.ValuationPreferences.ValuationDate),
            request.ValuationPreferences.ShowIncome,
            request.ValuationPreferences.ShowBook);

    private static Guid? GetAccountID(IAccountEvent @event) =>
        @event switch
        {
            AccountCreatedEvent createdEvent => createdEvent.AccountID.Value,
            AccountModifiedEvent modifiedEvent => modifiedEvent.AccountID.Value,
            AccountActiveSetEvent activeEvent => activeEvent.AccountID.Value,
            AccountDisplayOrderSetEvent displayOrderSetEvent => displayOrderSetEvent.AccountID.Value,
            _ => null
        };

    private static string? GetBrokerLEI(IBrokerEvent @event) =>
        @event switch
        {
            BrokerCreatedEvent createdEvent => createdEvent.LEI.Value,
            BrokerModifiedEvent modifiedEvent => modifiedEvent.LEI.Value,
            BrokerActiveSetEvent activeEvent => activeEvent.LEI.Value,
            BrokerApprovedDateTimeSetEvent approvedEvent => approvedEvent.LEI.Value,
            BrokerNextReviewSetEvent nextReviewEvent => nextReviewEvent.LEI.Value,
            BrokerNotesSetEvent notesEvent => notesEvent.LEI.Value,
            _ => null
        };

    private static string? GetCountryAlpha2(ICountryEvent @event) =>
        @event switch
        {
            CountryCreatedEvent createdEvent => createdEvent.Alpha2.Value,
            CountryModifiedEvent modifiedEvent => modifiedEvent.Alpha2.Value,
            CountryFlagModifiedEvent flagEvent => flagEvent.Alpha2.Value,
            _ => null
        };

    private static string? GetCurrencyAlphabeticCode(ICurrencyEvent @event) =>
        @event switch
        {
            CurrencyCreatedEvent createdEvent => createdEvent.AlphabeticCode.Value,
            CurrencyModifiedEvent modifiedEvent => modifiedEvent.AlphabeticCode.Value,
            _ => null
        };

    private static Guid? GetAssetAllocationID(IValuationSettingEvent @event) =>
        @event.AssetAllocationID?.Value;

    private static string? GetFXPair(IFXEvent @event) =>
        @event switch
        {
            FXCreatedEvent createdEvent => createdEvent.Pair.Value,
            FXActiveModifiedEvent activeEvent => activeEvent.Pair.Value,
            _ => null
        };

    private static string? GetFXRatePair(IFXRateEvent @event) =>
        @event is FXRateSetEvent setEvent
            ? setEvent.Pair.Value
            : null;

    private static Guid? GetInstrumentID(IInstrumentEvent @event) =>
        @event switch
        {
            InstrumentCreatedEvent createdEvent => createdEvent.InstrumentID.Value,
            InstrumentModifiedEvent modifiedEvent => modifiedEvent.InstrumentID.Value,
            InstrumentActiveModifiedEvent activeEvent => activeEvent.InstrumentID.Value,
            InstrumentIdentifierSetEvent setEvent => setEvent.InstrumentID.Value,
            InstrumentIdentifierUnsetEvent unsetEvent => unsetEvent.InstrumentID.Value,
            InstrumentTermsSetEvent termsEvent => termsEvent.InstrumentID.Value,
            _ => null
        };

    private static object CreateAcceptedEventsResponse(string eventRoute, IReadOnlyList<IEventBase> events) =>
        new
        {
            EventIDs = events.Select(@event => @event.EventID.Value).ToList(),
            Links = events
                .Select(@event => new
                {
                    Rel = "self",
                    Href = $"{eventRoute}/{@event.EventID.Value}",
                    Method = "GET"
                })
                .ToList()
        };

    private static object CreateAcceptedEventsResponse(IReadOnlyList<(string EventRoute, IEventBase Event)> events) =>
        new
        {
            EventIDs = events.Select(item => item.Event.EventID.Value).ToList(),
            Links = events
                .Select(item => new
                {
                    Rel = "self",
                    Href = $"{item.EventRoute}/{item.Event.EventID.Value}",
                    Method = "GET"
                })
                .ToList()
        };

    private static async Task<IResult> AppendTicketEvent<TEvent>(
        IEventRepository eventRepository,
        AggregateCacheInvalidationService cacheInvalidationService,
        Func<Task<Result<TEvent>>> createEvent,
        CancellationToken cancellationToken)
        where TEvent : class, ITicket
    {
        var result = await createEvent();
        if (!result.IsValid || result.Value is null)
            return Results.BadRequest(result);

        await eventRepository.AppendAsync(Constants.Initialisation.TicketsStreamId, result.Value, cancellationToken);
        cacheInvalidationService.Invalidate(result.Value);

        return Results.Accepted(TicketEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(TicketEventsRoute, result.Value));
    }

    private static async Task<IReadOnlyList<string>> ValidateInstrumentValueEvent(InstrumentID instrumentID, EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var instrumentEvents = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId, cancellationToken);
        var instruments = new Instruments(eventDateTime, auditDateTime, instrumentEvents.ToList());
        var instrument = instruments.Items.SingleOrDefault(item => item.InstrumentID == instrumentID);
        if (instrument is null)
            return [$"No matching Instrument found for InstrumentID '{instrumentID}'."];

        return instrument.CFI.IsEquity || instrument.CFI.IsDebt
            ? []
            : [$"Instrument '{instrumentID}' has CFI '{instrument.CFI.Value}'. Only equity and fixed income instruments support editable v1 price and income events."];
    }

    private static async Task<Accounts?> TryGetAccounts(EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var accountEvents = await eventRepository.LoadStreamAsync<IAccountEvent>(Constants.Initialisation.AccountsStreamId, cancellationToken);
        return accountEvents.Count == 0 ? null : new Accounts(eventDateTime, auditDateTime, accountEvents.ToList());
    }

    private static async Task<Holdings?> TryGetHoldings(EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var holdingEvents = await eventRepository.LoadStreamAsync<IHoldingEvent>(Constants.Initialisation.HoldingsStreamId, cancellationToken);
        return holdingEvents.Count == 0 ? null : new Holdings(eventDateTime, auditDateTime, holdingEvents.ToList());
    }

    private static async Task<ValuationSettings?> TryGetValuationSettings(EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var valuationSettingEvents = await eventRepository.LoadStreamAsync<IValuationSettingEvent>(Constants.Initialisation.ValuationSettingsStreamId, cancellationToken);
        return valuationSettingEvents.Count == 0 ? null : new ValuationSettings(eventDateTime, auditDateTime, valuationSettingEvents.ToList());
    }

    private static async Task<ReportConfigs?> TryGetReportConfigs(EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var reportEvents = await eventRepository.LoadStreamAsync<IReportEvent>(Constants.Initialisation.ReportConfigsStreamId, cancellationToken);
        return reportEvents.Count == 0 ? null : new ReportConfigs(eventDateTime, auditDateTime, reportEvents.ToList());
    }

    private static ApiExchangeResponse ToResponse(ApiExchange exchange) =>
        new(
            exchange.Id,
            exchange.StartedAtUtc,
            exchange.CompletedAtUtc,
            exchange.DurationMilliseconds,
            exchange.Method,
            exchange.Path,
            exchange.QueryString,
            exchange.StatusCode,
            exchange.ExceptionType,
            exchange.ExceptionMessage,
            ToResponse(exchange.Request),
            ToResponse(exchange.Response));

    private static ApiHttpMessageResponse ToResponse(ApiHttpMessage message) =>
        new(message.Headers, message.Body, message.ContentType, message.ContentLength, message.BodyTruncated);

    private static FIXOperationResponse ToResponse(FoleoTraderFIXOperationRecordedEvent @event) =>
        new(
            @event.EventID.Value,
            @event.AuditDateTime.Value,
            @event.EventDateTime.Value,
            @event.AuditDateTime.Value,
            @event.Reason,
            @event.Direction,
            @event.Channel,
            @event.SessionID,
            @event.MsgType,
            FIXMessageName(@event.MsgType),
            @event.MsgSeqNum,
            @event.SenderCompID,
            @event.TargetCompID,
            @event.SendingTime,
            @event.ClOrdID,
            @event.ExecID,
            @event.RawMessage,
            @event.RawMessage.Replace('\u0001', '|'));

    private static bool Matches(string value, string? filter) =>
        string.IsNullOrWhiteSpace(filter) || string.Equals(value, filter, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string value, string? filter) =>
        string.IsNullOrWhiteSpace(filter) || value.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesFIXText(FoleoTraderFIXOperationRecordedEvent @event, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        return Contains(@event.RawMessage, text) ||
            Contains(@event.RawMessage.Replace('\u0001', '|'), text) ||
            Contains(@event.SessionID, text) ||
            Contains(@event.Direction, text) ||
            Contains(@event.Channel, text) ||
            Contains(@event.MsgType, text) ||
            Contains(FIXMessageName(@event.MsgType), text) ||
            Contains(@event.SenderCompID, text) ||
            Contains(@event.TargetCompID, text) ||
            Contains(@event.ClOrdID, text) ||
            Contains(@event.ExecID, text);
    }

    private static string FIXMessageName(string msgType) =>
        msgType switch
        {
            "0" => "Heartbeat",
            "1" => "Test Request",
            "2" => "Resend Request",
            "3" => "Reject",
            "4" => "Sequence Reset",
            "5" => "Logout",
            "8" => "Execution Report",
            "9" => "Order Cancel Reject",
            "A" => "Logon",
            "D" => "New Order Single",
            "F" => "Order Cancel Request",
            "G" => "Order Cancel/Replace Request",
            "j" => "Business Message Reject",
            _ => string.IsNullOrWhiteSpace(msgType) ? "Unknown" : msgType
        };

    private static bool IsApiExchangeStoreUnavailable(Exception exception) =>
        exception is TimeoutException ||
        exception.GetType().FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true ||
        exception.InnerException is not null && IsApiExchangeStoreUnavailable(exception.InnerException);

    private static IResult RequestTraceUnavailable() =>
        Results.Problem(
            title: "Request trace store is unavailable.",
            detail: "The API exchange capture database cannot be reached. Start the configured Postgres instance or update ConnectionStrings:FolioTrace.",
            statusCode: StatusCodes.Status503ServiceUnavailable);

    private static object ToAccountEventResponse(IAccountEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            AccountCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                AccountID = createdEvent.AccountID.Value,
                createdEvent.Name,
                createdEvent.FormalName,
                BookCurrency = createdEvent.BookCurrency.Value,
                createdEvent.Active
            },
            AccountModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                AccountID = modifiedEvent.AccountID.Value,
                modifiedEvent.Name,
                modifiedEvent.FormalName
            },
            AccountActiveSetEvent activeEvent => new
            {
                Type = activeEvent.Type,
                EventID = activeEvent.EventID.Value,
                UserID = activeEvent.UserID.Value,
                EventDateTime = activeEvent.EventDateTime.Value,
                AuditDateTime = activeEvent.AuditDateTime.Value,
                activeEvent.Reason,
                AccountID = activeEvent.AccountID.Value,
                activeEvent.Active
            },
            AccountDisplayOrderSetEvent displayOrderSetEvent => new
            {
                Type = displayOrderSetEvent.Type,
                EventID = displayOrderSetEvent.EventID.Value,
                UserID = displayOrderSetEvent.UserID.Value,
                EventDateTime = displayOrderSetEvent.EventDateTime.Value,
                AuditDateTime = displayOrderSetEvent.AuditDateTime.Value,
                displayOrderSetEvent.Reason,
                AccountID = displayOrderSetEvent.AccountID.Value,
                DisplayOrder = displayOrderSetEvent.DisplayOrder.Value
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToBrokerEventResponse(IBrokerEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            BrokerCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                createdEvent.Name,
                LEI = createdEvent.LEI.Value,
                Commission = createdEvent.Commission.Value,
                createdEvent.Active,
                ApprovedDateTime = createdEvent.ApprovedDateTime.Value,
                NextReview = createdEvent.NextReview.Value,
                createdEvent.Notes
            },
            BrokerModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                LEI = modifiedEvent.LEI.Value,
                modifiedEvent.Name,
                Commission = modifiedEvent.Commission.Value
            },
            BrokerActiveSetEvent activeSetEvent => new
            {
                Type = activeSetEvent.Type,
                EventID = activeSetEvent.EventID.Value,
                UserID = activeSetEvent.UserID.Value,
                EventDateTime = activeSetEvent.EventDateTime.Value,
                AuditDateTime = activeSetEvent.AuditDateTime.Value,
                activeSetEvent.Reason,
                LEI = activeSetEvent.LEI.Value,
                activeSetEvent.Active
            },
            BrokerApprovedDateTimeSetEvent approvedDateTimeSetEvent => new
            {
                Type = approvedDateTimeSetEvent.Type,
                EventID = approvedDateTimeSetEvent.EventID.Value,
                UserID = approvedDateTimeSetEvent.UserID.Value,
                EventDateTime = approvedDateTimeSetEvent.EventDateTime.Value,
                AuditDateTime = approvedDateTimeSetEvent.AuditDateTime.Value,
                approvedDateTimeSetEvent.Reason,
                LEI = approvedDateTimeSetEvent.LEI.Value,
                ApprovedDateTime = approvedDateTimeSetEvent.ApprovedDateTime.Value
            },
            BrokerNextReviewSetEvent nextReviewSetEvent => new
            {
                Type = nextReviewSetEvent.Type,
                EventID = nextReviewSetEvent.EventID.Value,
                UserID = nextReviewSetEvent.UserID.Value,
                EventDateTime = nextReviewSetEvent.EventDateTime.Value,
                AuditDateTime = nextReviewSetEvent.AuditDateTime.Value,
                nextReviewSetEvent.Reason,
                LEI = nextReviewSetEvent.LEI.Value,
                NextReview = nextReviewSetEvent.NextReview.Value
            },
            BrokerNotesSetEvent notesSetEvent => new
            {
                Type = notesSetEvent.Type,
                EventID = notesSetEvent.EventID.Value,
                UserID = notesSetEvent.UserID.Value,
                EventDateTime = notesSetEvent.EventDateTime.Value,
                AuditDateTime = notesSetEvent.AuditDateTime.Value,
                notesSetEvent.Reason,
                LEI = notesSetEvent.LEI.Value,
                notesSetEvent.Notes
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToCountryEventResponse(ICountryEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            CountryCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                Alpha2 = createdEvent.Alpha2.Value,
                Alpha3 = createdEvent.Alpha3.Value,
                createdEvent.Numeric,
                createdEvent.Name,
                Flag = (CountryFlag?)null
            },
            CountryModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                Alpha2 = modifiedEvent.Alpha2.Value,
                Alpha3 = modifiedEvent.Alpha3.Value,
                modifiedEvent.Numeric,
                modifiedEvent.Name,
                Flag = (CountryFlag?)null
            },
            CountryFlagModifiedEvent flagModifiedEvent => new
            {
                Type = flagModifiedEvent.Type,
                EventID = flagModifiedEvent.EventID.Value,
                UserID = flagModifiedEvent.UserID.Value,
                EventDateTime = flagModifiedEvent.EventDateTime.Value,
                AuditDateTime = flagModifiedEvent.AuditDateTime.Value,
                flagModifiedEvent.Reason,
                Alpha2 = flagModifiedEvent.Alpha2.Value,
                Alpha3 = (string?)null,
                Numeric = (short?)null,
                Name = (string?)null,
                flagModifiedEvent.Flag
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToCurrencyEventResponse(ICurrencyEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            CurrencyCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                AlphabeticCode = createdEvent.AlphabeticCode.Value,
                createdEvent.NumericCode,
                createdEvent.DecimalPlace,
                createdEvent.Name
            },
            CurrencyModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                AlphabeticCode = modifiedEvent.AlphabeticCode.Value,
                modifiedEvent.NumericCode,
                modifiedEvent.DecimalPlace,
                modifiedEvent.Name
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToValuationSettingEventResponse(IValuationSettingEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            AssetAllocationCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                AssetAllocationID = createdEvent.AssetAllocationID.Value,
                createdEvent.Name,
                AccountIDs = createdEvent.AccountIDs.Select(accountID => accountID.Value).ToList(),
                createdEvent.Active,
                RootNodeID = createdEvent.RootNodeID.Value,
                Nodes = ToAssetAllocationNodeResponses(createdEvent.Nodes)
            },
            AssetAllocationModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                AssetAllocationID = modifiedEvent.AssetAllocationID.Value,
                modifiedEvent.Name,
                RootNodeID = modifiedEvent.RootNodeID.Value,
                Nodes = ToAssetAllocationNodeResponses(modifiedEvent.Nodes)
            },
            AssetAllocationAccountIDsSetEvent accountIDsSetEvent => new
            {
                Type = accountIDsSetEvent.Type,
                EventID = accountIDsSetEvent.EventID.Value,
                UserID = accountIDsSetEvent.UserID.Value,
                AuditDateTime = accountIDsSetEvent.AuditDateTime.Value,
                AssetAllocationID = accountIDsSetEvent.AssetAllocationID.Value,
                AccountIDs = accountIDsSetEvent.AccountIDs.Select(accountID => accountID.Value).ToList()
            },
            AssetAllocationActiveSetEvent activeSetEvent => new
            {
                Type = activeSetEvent.Type,
                EventID = activeSetEvent.EventID.Value,
                UserID = activeSetEvent.UserID.Value,
                AuditDateTime = activeSetEvent.AuditDateTime.Value,
                AssetAllocationID = activeSetEvent.AssetAllocationID.Value,
                activeSetEvent.Active
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                AssetAllocationID = @event.AssetAllocationID.Value
            }
        });

    private static List<object> ToAssetAllocationNodeResponses(IEnumerable<AssetAllocationNode> nodes) =>
        nodes
            .Select(node => new
            {
                NodeID = node.NodeID.Value,
                Nodes = node.Nodes.Select(nodeID => nodeID.Value).ToList(),
                node.Name,
                node.Subtotal,
                node.Hidden,
                node.Colour,
                AccountSettings = node.AccountSettings.Select(setting => new
                {
                    AccountID = setting.AccountID.Value,
                    setting.TargetWeight,
                    setting.TargetWeightMax,
                    setting.TargetWeightMin,
                    setting.TargetYield
                }).ToList()
            })
            .Cast<object>()
            .ToList();

    private static object ToAssetAllocationMappingEventResponse(IAssetAllocationMappingEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            Type = @event.Type,
            EventID = @event.EventID.Value,
            UserID = @event.UserID.Value,
            EventDateTime = @event.EventDateTime.Value,
            AuditDateTime = @event.AuditDateTime.Value,
            @event.Reason,
            AssetAllocationID = @event.AssetAllocationID.Value,
            HoldingID = @event.HoldingID.Value,
            NodeID = @event.NodeID.Value
        });

    private static object ToReportEventResponse(IReportEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            ReportCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                ReportID = createdEvent.ReportID.Value,
                createdEvent.Name,
                createdEvent.Active,
                Nodes = ToReportNodeResponses(createdEvent.Nodes)
            },
            ReportModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                ReportID = modifiedEvent.ReportID.Value,
                modifiedEvent.Name,
                modifiedEvent.Active,
                Nodes = ToReportNodeResponses(modifiedEvent.Nodes)
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                ReportID = @event.ReportID.Value
            }
        });

    private static List<object> ToReportNodeResponses(IEnumerable<ReportNodeBase> nodes) =>
        nodes
            .OrderBy(node => node.DisplayOrder)
            .Select(ToReportNodeResponse)
            .ToList();

    private static object ToReportNodeResponse(ReportNodeBase node) =>
        node switch
        {
            ReportNodeChart chart => new
            {
                Type = nameof(ReportNodeChart),
                ReportNodeID = chart.ReportNodeID.Value,
                chart.DisplayOrder,
                chart.Name,
                chart.Title,
                PageOrientation = chart.PageOrientation.ToString(),
                AssetAllocationID = chart.AssetAllocationID.Value,
                ChartType = chart.ChartType.ToString(),
                chart.PieLevel
            },
            ReportNodeValuation valuation => new
            {
                Type = nameof(ReportNodeValuation),
                ReportNodeID = valuation.ReportNodeID.Value,
                valuation.DisplayOrder,
                valuation.Name,
                valuation.Title,
                PageOrientation = valuation.PageOrientation.ToString(),
                AssetAllocationID = valuation.AssetAllocationID.Value,
                Columns = ToReportValuationColumnResponses(valuation.Columns),
                valuation.ColourBullet,
                valuation.ColourText,
                valuation.DisplayHoldings
            },
            ReportNodeTransactions transactions => new
            {
                Type = nameof(ReportNodeTransactions),
                ReportNodeID = transactions.ReportNodeID.Value,
                transactions.DisplayOrder,
                transactions.Name,
                transactions.Title,
                PageOrientation = transactions.PageOrientation.ToString(),
                AssetAllocationID = transactions.AssetAllocationID.Value
            },
            ReportNodeCash cash => new
            {
                Type = nameof(ReportNodeCash),
                ReportNodeID = cash.ReportNodeID.Value,
                cash.DisplayOrder,
                cash.Name,
                cash.Title,
                PageOrientation = cash.PageOrientation.ToString(),
                AssetAllocationID = cash.AssetAllocationID.Value
            },
            _ => new
            {
                Type = node.GetType().Name,
                ReportNodeID = node.ReportNodeID.Value,
                node.DisplayOrder,
                node.Name,
                node.Title,
                PageOrientation = node.PageOrientation.ToString()
            }
        };

    private static List<object> ToReportValuationColumnResponses(IEnumerable<ReportValuationColumn>? columns) =>
        ReportConfigBuilder.NormaliseValuationColumns(columns)
            .Select(column => new
            {
                ColumnKey = column.ColumnKey.ToString(),
                column.DisplayOrder
            })
            .Cast<object>()
            .ToList();

    private static object ToHoldingEventResponse(IHoldingEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            HoldingCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                HoldingID = createdEvent.HoldingID.Value,
                AccountID = createdEvent.AccountID.Value,
                InstrumentID = createdEvent.InstrumentID.Value,
                HoldingKind = createdEvent.GetHoldingKindName(),
                createdEvent.Name,
                createdEvent.Active,
                createdEvent.Default,
                BankName = createdEvent is HoldingCashBaseCreatedEvent bankEvent ? bankEvent.BankName : null,
                AccountName = createdEvent is HoldingCashBaseCreatedEvent accountEvent ? accountEvent.AccountName : null,
                SortCode = createdEvent is HoldingCashBaseCreatedEvent sortEvent ? sortEvent.SortCode.Value : null,
                AccountNumber = createdEvent is HoldingCashBaseCreatedEvent numberEvent ? numberEvent.AccountNumber.Value : null,
                BIC = createdEvent is HoldingCashBaseCreatedEvent bicEvent ? bicEvent.BIC.Value : null,
                IBAN = createdEvent is HoldingCashBaseCreatedEvent ibanEvent ? ibanEvent.IBAN.Value : null
            },
            HoldingModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                HoldingID = modifiedEvent.HoldingID.Value,
                HoldingKind = modifiedEvent.GetHoldingKindName(),
                modifiedEvent.Name,
                modifiedEvent.Default,
                BankName = modifiedEvent is HoldingCashBaseModifiedEvent bankEvent ? bankEvent.BankName : null,
                AccountName = modifiedEvent is HoldingCashBaseModifiedEvent accountEvent ? accountEvent.AccountName : null,
                SortCode = modifiedEvent is HoldingCashBaseModifiedEvent sortEvent ? sortEvent.SortCode.Value : null,
                AccountNumber = modifiedEvent is HoldingCashBaseModifiedEvent numberEvent ? numberEvent.AccountNumber.Value : null,
                BIC = modifiedEvent is HoldingCashBaseModifiedEvent bicEvent ? bicEvent.BIC.Value : null,
                IBAN = modifiedEvent is HoldingCashBaseModifiedEvent ibanEvent ? ibanEvent.IBAN.Value : null
            },
            HoldingActiveModifiedEvent activeEvent => new
            {
                Type = activeEvent.Type,
                EventID = activeEvent.EventID.Value,
                UserID = activeEvent.UserID.Value,
                EventDateTime = activeEvent.EventDateTime.Value,
                AuditDateTime = activeEvent.AuditDateTime.Value,
                activeEvent.Reason,
                HoldingID = activeEvent.HoldingID.Value,
                activeEvent.Active
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToFXEventResponse(IFXEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            FXCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                Pair = createdEvent.Pair.Value,
                DisplayPair = createdEvent.Pair.DisplayValue,
                BaseCurrency = createdEvent.BaseCurrency.Value,
                QuoteCurrency = createdEvent.QuoteCurrency.Value,
                createdEvent.Active
            },
            FXActiveModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                Pair = modifiedEvent.Pair.Value,
                DisplayPair = modifiedEvent.Pair.DisplayValue,
                BaseCurrency = modifiedEvent.Pair.BaseCurrency.Value,
                QuoteCurrency = modifiedEvent.Pair.QuoteCurrency.Value,
                modifiedEvent.Active
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToFXRateEventResponse(IFXRateEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            FXRateSetEvent setEvent => new
            {
                Type = setEvent.Type,
                EventID = setEvent.EventID.Value,
                UserID = setEvent.UserID.Value,
                EventDateTime = setEvent.EventDateTime.Value,
                AuditDateTime = setEvent.AuditDateTime.Value,
                setEvent.Reason,
                Pair = setEvent.Pair.Value,
                DisplayPair = setEvent.Pair.DisplayValue,
                setEvent.Price
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToInstrumentEventResponse(IInstrumentEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            InstrumentCreatedEvent createdEvent => new
            {
                Type = createdEvent.Type,
                EventID = createdEvent.EventID.Value,
                UserID = createdEvent.UserID.Value,
                EventDateTime = createdEvent.EventDateTime.Value,
                AuditDateTime = createdEvent.AuditDateTime.Value,
                createdEvent.Reason,
                InstrumentID = createdEvent.InstrumentID.Value,
                createdEvent.Name,
                createdEvent.FormalName,
                Exchange = createdEvent.Exchange.Value,
                CFI = createdEvent.CFI.Value,
                createdEvent.Logo,
                createdEvent.Active,
                IncomeCountry = createdEvent.IncomeCountry.Value,
                PriceCountry = createdEvent.PriceCountry.Value,
                PriceCurrency = createdEvent.PriceCurrency.Value
            },
            InstrumentModifiedEvent modifiedEvent => new
            {
                Type = modifiedEvent.Type,
                EventID = modifiedEvent.EventID.Value,
                UserID = modifiedEvent.UserID.Value,
                EventDateTime = modifiedEvent.EventDateTime.Value,
                AuditDateTime = modifiedEvent.AuditDateTime.Value,
                modifiedEvent.Reason,
                InstrumentID = modifiedEvent.InstrumentID.Value,
                modifiedEvent.Name,
                modifiedEvent.FormalName,
                Exchange = modifiedEvent.Exchange.Value,
                CFI = modifiedEvent.CFI.Value,
                modifiedEvent.Logo,
                IncomeCountry = modifiedEvent.IncomeCountry.Value,
                PriceCountry = modifiedEvent.PriceCountry.Value,
                PriceCurrency = modifiedEvent.PriceCurrency.Value
            },
            InstrumentActiveModifiedEvent activeEvent => new
            {
                Type = activeEvent.Type,
                EventID = activeEvent.EventID.Value,
                UserID = activeEvent.UserID.Value,
                EventDateTime = activeEvent.EventDateTime.Value,
                AuditDateTime = activeEvent.AuditDateTime.Value,
                activeEvent.Reason,
                InstrumentID = activeEvent.InstrumentID.Value,
                activeEvent.Active
            },
            InstrumentIdentifierSetEvent setEvent => new
            {
                Type = setEvent.Type,
                EventID = setEvent.EventID.Value,
                UserID = setEvent.UserID.Value,
                EventDateTime = setEvent.EventDateTime.Value,
                AuditDateTime = setEvent.AuditDateTime.Value,
                setEvent.Reason,
                InstrumentID = setEvent.InstrumentID.Value,
                setEvent.Identifier
            },
            InstrumentIdentifierUnsetEvent unsetEvent => new
            {
                Type = unsetEvent.Type,
                EventID = unsetEvent.EventID.Value,
                UserID = unsetEvent.UserID.Value,
                EventDateTime = unsetEvent.EventDateTime.Value,
                AuditDateTime = unsetEvent.AuditDateTime.Value,
                unsetEvent.Reason,
                InstrumentID = unsetEvent.InstrumentID.Value,
                unsetEvent.IdentifierType
            },
            InstrumentTermsSetEvent termsEvent => new
            {
                Type = termsEvent.Type,
                EventID = termsEvent.EventID.Value,
                UserID = termsEvent.UserID.Value,
                EventDateTime = termsEvent.EventDateTime.Value,
                AuditDateTime = termsEvent.AuditDateTime.Value,
                termsEvent.Reason,
                InstrumentID = termsEvent.InstrumentID.Value,
                termsEvent.Terms
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToInstrumentPriceEventResponse(IInstrumentPriceEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            InstrumentPriceSetEvent setEvent => new
            {
                Type = setEvent.Type,
                EventID = setEvent.EventID.Value,
                UserID = setEvent.UserID.Value,
                EventDateTime = setEvent.EventDateTime.Value,
                AuditDateTime = setEvent.AuditDateTime.Value,
                setEvent.Reason,
                InstrumentID = setEvent.InstrumentID.Value,
                setEvent.Price
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToInstrumentIncomeEventResponse(IInstrumentIncomeEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            InstrumentIncomeSetEvent setEvent => new
            {
                Type = setEvent.Type,
                EventID = setEvent.EventID.Value,
                UserID = setEvent.UserID.Value,
                EventDateTime = setEvent.EventDateTime.Value,
                AuditDateTime = setEvent.AuditDateTime.Value,
                setEvent.Reason,
                InstrumentID = setEvent.InstrumentID.Value,
                setEvent.Income
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToTransactionEventResponse(ITransactionEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, @event switch
        {
            TransactionCreditEvent creditEvent => new
            {
                Type = creditEvent.Type,
                EventID = creditEvent.EventID.Value,
                UserID = creditEvent.UserID.Value,
                EventDateTime = creditEvent.EventDateTime.Value,
                SettlementDateTime = creditEvent.SettlementDateTime.Value,
                AuditDateTime = creditEvent.AuditDateTime.Value,
                creditEvent.Reason,
                EventSetID = creditEvent.EventSetID.Value,
                EventIDGroup = creditEvent.EventIDGroup.Select(eventId => eventId.Value).ToList(),
                HoldingID = creditEvent.HoldingID?.Value,
                InstrumentID = creditEvent.InstrumentID.Value,
                AccountID = creditEvent.AccountID.Value,
                Quantity = creditEvent.Quantity.Value,
                LocalCost = creditEvent.LocalCost.Value,
                LocalCostCurrency = creditEvent.LocalCostCurrency.Value,
                BookCost = creditEvent.BookCost.Value,
                BookCostSource = creditEvent.BookCostSource.ToString(),
                creditEvent.BookCostEstimated
            },
            TransactionDebitEvent debitEvent => new
            {
                Type = debitEvent.Type,
                EventID = debitEvent.EventID.Value,
                UserID = debitEvent.UserID.Value,
                EventDateTime = debitEvent.EventDateTime.Value,
                SettlementDateTime = debitEvent.SettlementDateTime.Value,
                AuditDateTime = debitEvent.AuditDateTime.Value,
                debitEvent.Reason,
                EventSetID = debitEvent.EventSetID.Value,
                EventIDGroup = debitEvent.EventIDGroup.Select(eventId => eventId.Value).ToList(),
                HoldingID = debitEvent.HoldingID?.Value,
                InstrumentID = debitEvent.InstrumentID.Value,
                AccountID = debitEvent.AccountID.Value,
                Quantity = debitEvent.Quantity.Value,
                LocalCost = debitEvent.LocalCost.Value,
                LocalCostCurrency = debitEvent.LocalCostCurrency.Value,
                BookCost = debitEvent.BookCost.Value,
                BookCostSource = debitEvent.BookCostSource.ToString(),
                debitEvent.BookCostEstimated
            },
            TransactionBookCostAdjustedEvent adjustmentEvent => new
            {
                Type = adjustmentEvent.Type,
                EventID = adjustmentEvent.EventID.Value,
                UserID = adjustmentEvent.UserID.Value,
                EventDateTime = adjustmentEvent.EventDateTime.Value,
                SettlementDateTime = adjustmentEvent.SettlementDateTime.Value,
                AuditDateTime = adjustmentEvent.AuditDateTime.Value,
                adjustmentEvent.Reason,
                EventSetID = adjustmentEvent.EventSetID.Value,
                AdjustedIDGroup = adjustmentEvent.AdjustedIDGroup.Select(eventId => eventId.Value).ToList(),
                AccountID = adjustmentEvent.AccountID.Value,
                BookCost = adjustmentEvent.BookCost.Value,
                BookCostSource = adjustmentEvent.BookCostSource.ToString(),
                adjustmentEvent.BookCostEstimated
            },
            TransactionCancellationEvent cancellationEvent => new
            {
                Type = cancellationEvent.Type,
                EventID = cancellationEvent.EventID.Value,
                UserID = cancellationEvent.UserID.Value,
                EventDateTime = cancellationEvent.EventDateTime.Value,
                SettlementDateTime = cancellationEvent.SettlementDateTime.Value,
                AuditDateTime = cancellationEvent.AuditDateTime.Value,
                cancellationEvent.Reason,
                EventSetID = cancellationEvent.EventSetID.Value,
                EventIDGroup = cancellationEvent.EventIDGroup.Select(eventId => eventId.Value).ToList(),
                AccountID = cancellationEvent.AccountID?.Value,
                CancelledEventID = cancellationEvent.CancelledEventID.Value,
                CancelledIDGroup = cancellationEvent.CancelledIDGroup.Select(eventId => eventId.Value).ToList()
            },
            _ => new
            {
                Type = @event.Type,
                EventID = @event.EventID.Value,
                UserID = @event.UserID.Value,
                EventDateTime = @event.EventDateTime.Value,
                AuditDateTime = @event.AuditDateTime.Value,
                @event.Reason
            }
        });

    private static object ToTicketEventResponse(ITicket @event) =>
        @event switch
        {
            TicketCreatedEvent createdEvent => WithTicketBase(createdEvent, new
            {
                Side = createdEvent.Side.ToString(),
                InstrumentID = createdEvent.InstrumentID.Value,
                TradeCurrency = createdEvent.TradeCurrency.Value
            }),
            TicketAccountAddedEvent accountAddedEvent => WithTicketBase(accountAddedEvent, new
            {
                AccountID = accountAddedEvent.AccountID.Value
            }),
            TicketAccountRemovedEvent accountRemovedEvent => WithTicketBase(accountRemovedEvent, new
            {
                AccountID = accountRemovedEvent.AccountID.Value
            }),
            TicketProposalCreatedEvent proposalCreatedEvent => WithTicketBase(proposalCreatedEvent, new
            {
                proposalCreatedEvent.TargetPrice,
                TradeCurrency = proposalCreatedEvent.TradeCurrency.Value,
                Allocations = proposalCreatedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketProposalModifiedEvent proposalModifiedEvent => WithTicketBase(proposalModifiedEvent, new
            {
                proposalModifiedEvent.TargetPrice,
                TradeCurrency = proposalModifiedEvent.TradeCurrency.Value,
                Allocations = proposalModifiedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketProposalReasonSetEvent proposalReasonSetEvent => WithTicketBase(proposalReasonSetEvent, new
            {
                proposalReasonSetEvent.ProposalReason
            }),
            TicketProposalAllocationSetEvent proposalAllocationSetEvent => WithTicketBase(proposalAllocationSetEvent, new
            {
                proposalAllocationSetEvent.ProposalAllocation
            }),
            TicketTradeCreatedEvent tradeCreatedEvent => WithTicketBase(tradeCreatedEvent, new
            {
                tradeCreatedEvent.TradedPrice,
                tradeCreatedEvent.TradeDateTime,
                tradeCreatedEvent.SettlementDateTime,
                Allocations = tradeCreatedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketTradeModifiedEvent tradeModifiedEvent => WithTicketBase(tradeModifiedEvent, new
            {
                tradeModifiedEvent.TradedPrice,
                tradeModifiedEvent.TradeDateTime,
                tradeModifiedEvent.SettlementDateTime,
                Allocations = tradeModifiedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketTradeFillAddedEvent fillAddedEvent => WithTicketBase(fillAddedEvent, new
            {
                fillAddedEvent.FillID,
                fillAddedEvent.BrokerLEI,
                fillAddedEvent.Price,
                fillAddedEvent.Quantity,
                fillAddedEvent.SettlementAmount,
                fillAddedEvent.BookCostOverride,
                fillAddedEvent.Note
            }),
            TicketTradeFillModifiedEvent fillModifiedEvent => WithTicketBase(fillModifiedEvent, new
            {
                fillModifiedEvent.FillID,
                fillModifiedEvent.BrokerLEI,
                fillModifiedEvent.Price,
                fillModifiedEvent.Quantity,
                fillModifiedEvent.SettlementAmount,
                fillModifiedEvent.BookCostOverride,
                fillModifiedEvent.Note
            }),
            TicketTradeFillRemovedEvent fillRemovedEvent => WithTicketBase(fillRemovedEvent, new
            {
                fillRemovedEvent.FillID
            }),
            TicketTradeDecisionRequestedEvent tradeDecisionRequestedEvent => WithTicketBase(tradeDecisionRequestedEvent, new { }),
            TicketTradeInstructionNotesSetEvent tradeInstructionNotesSetEvent => WithTicketBase(tradeInstructionNotesSetEvent, new
            {
                tradeInstructionNotesSetEvent.TradeInstructionNotes
            }),
            TicketTradeProgressNotesSetEvent tradeProgressNotesSetEvent => WithTicketBase(tradeProgressNotesSetEvent, new
            {
                tradeProgressNotesSetEvent.TradeProgressNotes
            }),
            _ => WithTicketBase(@event, new { })
        };

    private static object WithTicketBase(ITicket @event, object details)
    {
        var response = new
        {
            Type = @event.Type,
            EventID = @event.EventID.Value,
            UserID = @event.UserID.Value,
            EventDateTime = @event.EventDateTime.Value,
            AuditDateTime = @event.AuditDateTime.Value,
            @event.Reason,
            TicketNumber = @event.TicketNumber.Value,
            Details = details
        };

        return EventPropertyDetailsFactory.WithPropertyDetails(@event, response, details);
    }

    private static object ToResponse(TicketProposalAllocation allocation) =>
        new
        {
            AccountID = allocation.AccountID.Value,
            allocation.Quantity
        };

    private static object ToResponse(TicketTradeAllocation allocation) =>
        new
        {
            AccountID = allocation.AccountID.Value,
            CashHoldingID = allocation.CashHoldingID?.Value,
            allocation.Quantity,
            allocation.SettlementAmount,
            allocation.BookCostOverride
        };
}
