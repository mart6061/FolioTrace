using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;
using System.Text.Json;

namespace API;

public static class ApiEndpointRegistration
{
    private static readonly JsonSerializerOptions NotificationJsonOptions = new(JsonSerializerDefaults.Web);

    private const string CountryEventsRoute = "/API/Events/Country";
    private const string CurrencyEventsRoute = "/API/Events/Currency";
    private const string FXEventsRoute = "/API/Events/FX";
    private const string FXRateEventsRoute = "/API/Events/FXRate";
    private const string InstrumentEventsRoute = "/API/Events/Instrument";
    private const string InstrumentPriceEventsRoute = "/API/Events/InstrumentPrice";
    private const string InstrumentIncomeEventsRoute = "/API/Events/InstrumentIncome";
    private const string UserEventsRoute = "/API/Events/User";

    public static WebApplication MapFolioTraceApi(this WebApplication app)
    {
        var api = app.MapGroup("");

        api.MapGet("/HelloWorld", () => "Hello World!");
        api.MapDiagnosticsEndpoints();
        api.MapNotificationEndpoints();
        api.MapSystemEndpoints();
        api.MapCountryEndpoints();
        api.MapCurrencyEndpoints();
        api.MapFXEndpoints();
        api.MapFXRateEndpoints();
        api.MapInstrumentEndpoints();
        api.MapInstrumentValueEndpoints();
        api.MapCountryEventEndpoints();
        api.MapCurrencyEventEndpoints();
        api.MapFXEventEndpoints();
        api.MapFXRateEventEndpoints();
        api.MapInstrumentEventEndpoints();
        api.MapInstrumentPriceEventEndpoints();
        api.MapInstrumentIncomeEventEndpoints();
        api.MapUserEventEndpoints();

        return app;
    }

    private static void MapNotificationEndpoints(this RouteGroupBuilder api)
    {
        var notifications = api.MapGroup("/Notifications");

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
        var diagnostics = api.MapGroup("/Diagnostics");

        diagnostics.MapGet("/Memory", (IEventRepository eventRepository, CountryService countryService, CurrencyService currencyService, FXService fxService, FXRateService fxRateService, InstrumentService instrumentService, InstrumentValueService instrumentValueService, AggregateUpdateNotificationService notificationService, AggregateMaintenanceCoordinator aggregateMaintenanceCoordinator) =>
        {
            var repositoryDiagnostics = eventRepository.GetCacheDiagnostics();
            var countryDiagnostics = countryService.GetDiagnostics();
            var currencyDiagnostics = currencyService.GetDiagnostics();
            var fxDiagnostics = fxService.GetDiagnostics();
            var fxRateDiagnostics = fxRateService.GetDiagnostics();
            var instrumentDiagnostics = instrumentService.GetDiagnostics();
            var instrumentValueDiagnostics = instrumentValueService.GetDiagnostics();
            var sseDiagnostics = notificationService.GetDiagnostics();
            var aggregateMaintenanceDiagnostics = aggregateMaintenanceCoordinator.GetDiagnostics();

            return Results.Ok(new MemoryDiagnosticsResponse(
                new EventCacheDiagnosticsResponse(
                    repositoryDiagnostics.IsLoaded,
                    repositoryDiagnostics.StreamCount,
                    repositoryDiagnostics.EventCount),
                new CountryServiceDiagnosticsResponse(
                    countryDiagnostics.CacheEntryCount,
                    countryDiagnostics.CountryCount),
                new CurrencyServiceDiagnosticsResponse(
                    currencyDiagnostics.CacheEntryCount,
                    currencyDiagnostics.CurrencyCount),
                new FXServiceDiagnosticsResponse(
                    fxDiagnostics.CacheEntryCount,
                    fxDiagnostics.FXCount),
                new FXRateServiceDiagnosticsResponse(
                    fxRateDiagnostics.CacheEntryCount,
                    fxRateDiagnostics.FXRateCount),
                new InstrumentServiceDiagnosticsResponse(
                    instrumentDiagnostics.CacheEntryCount,
                    instrumentDiagnostics.InstrumentCount),
                new InstrumentValueServiceDiagnosticsResponse(
                    instrumentValueDiagnostics.CacheEntryCount,
                    instrumentValueDiagnostics.InstrumentValueCount),
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
        });

        diagnostics.MapGet("/RequestTrace/{id:guid}", async (Guid id, IApiExchangeRepository repository, CancellationToken cancellationToken) =>
        {
            var exchange = await repository.LoadAsync(id, cancellationToken);

            return exchange is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(exchange));
        });
    }

    private static void MapSystemEndpoints(this RouteGroupBuilder api)
    {
        var system = api.MapGroup("/System");

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
            IInstrumentValueReadModelRepository instrumentValueReadModelRepository,
            CancellationToken cancellationToken) =>
        {
            await using var maintenanceSuspension = await aggregateMaintenanceCoordinator.SuspendAsync("Clearing caches and projections.", cancellationToken);

            var removedCacheViews = cacheClearService.ClearAll();
            await fxRateReadModelRepository.ClearAsync(cancellationToken);
            await instrumentValueReadModelRepository.ClearAsync(cancellationToken);
            notificationService.PublishAggregatesInvalidated("Caches and projections cleared.");

            return Results.Ok(new
            {
                Status = "Complete",
                Message = "Caches and projections cleared.",
                RemovedCacheViews = removedCacheViews,
                ClearedProjections = new[]
                {
                    nameof(FXDefinitionReadModel),
                    nameof(FXRatePointReadModel),
                    nameof(InstrumentDefinitionReadModel),
                    nameof(InstrumentPricePointReadModel),
                    nameof(InstrumentIncomePointReadModel)
                }
            });
        });
    }

    private static void MapCountryEndpoints(this RouteGroupBuilder api)
    {
        var countries = api.MapGroup("/Countries");

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
        var currencies = api.MapGroup("/Currencies");

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
        var fxs = api.MapGroup("/FXs");

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
        var fxRates = api.MapGroup("/FXRates");

        fxRates.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, FXRateService fxRateService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await fxRateService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await fxRateService.Get(valuationDate));
        });
    }

    private static void MapInstrumentEndpoints(this RouteGroupBuilder api)
    {
        var instruments = api.MapGroup("/Instruments");

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
        var instrumentValues = api.MapGroup("/InstrumentValues");

        instrumentValues.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, InstrumentValueService instrumentValueService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await instrumentValueService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await instrumentValueService.Get(valuationDate));
        });
    }

    private static void MapCountryEventEndpoints(this RouteGroupBuilder api)
    {
        var countryEvents = api.MapGroup("/Events/Country");

        countryEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<ICountryEvent>(Constants.Initialisation.CountriesStreamId, cancellationToken);
            return Results.Ok(events.Select(ToCountryEventResponse).ToList());
        });

        countryEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is ICountryEvent
                ? Results.Ok(@event)
                : Results.NotFound();
        });

        countryEvents.MapPost("/", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, IEnumerable<IEventBase> events, CancellationToken cancellationToken) =>
        {
            var eventData = events.ToList();
            if (eventData.Any(@event => @event is not ICountryEvent))
                return Results.BadRequest("All events must be country events.");

            await eventRepository.AppendAsync(Constants.Initialisation.CountriesStreamId, eventData, cancellationToken);
            cacheInvalidationService.Invalidate(eventData);

            return Results.Accepted(
                CountryEventsRoute,
                eventData.Select(@event => EventEndpointFactory.CreateAcceptedEventResponse(CountryEventsRoute, @event)));
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
        var currencyEvents = api.MapGroup("/Events/Currency");

        currencyEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<ICurrencyEvent>(Constants.Initialisation.CurrenciesStreamId, cancellationToken);
            return Results.Ok(events.Select(ToCurrencyEventResponse).ToList());
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

    private static void MapFXEventEndpoints(this RouteGroupBuilder api)
    {
        var fxEvents = api.MapGroup("/Events/FX");

        fxEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId, cancellationToken);
            return Results.Ok(events.Select(ToFXEventResponse).ToList());
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
        var fxRateEvents = api.MapGroup("/Events/FXRate");

        fxRateEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IFXRateEvent>(Constants.Initialisation.FXRatesStreamId, cancellationToken);
            return Results.Ok(events.Select(ToFXRateEventResponse).ToList());
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

    private static void MapInstrumentEventEndpoints(this RouteGroupBuilder api)
    {
        var instrumentEvents = api.MapGroup("/Events/Instrument");

        instrumentEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId, cancellationToken);
            return Results.Ok(events.Select(ToInstrumentEventResponse).ToList());
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
        var priceEvents = api.MapGroup("/Events/InstrumentPrice");

        priceEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IInstrumentPriceEvent>(Constants.Initialisation.InstrumentPricesStreamId, cancellationToken);
            return Results.Ok(events.Select(ToInstrumentPriceEventResponse).ToList());
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
        var incomeEvents = api.MapGroup("/Events/InstrumentIncome");

        incomeEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IInstrumentIncomeEvent>(Constants.Initialisation.InstrumentIncomesStreamId, cancellationToken);
            return Results.Ok(events.Select(ToInstrumentIncomeEventResponse).ToList());
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

    private static void MapUserEventEndpoints(this RouteGroupBuilder api)
    {
        var userEvents = api.MapGroup("/Events/User");

        userEvents.MapPost($"/{nameof(UserCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserEventRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.UsersStreamId,
                UserEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => UserCreatedEventBuilder.Create(
                    request.UserID,
                    EventDateTimeBuilder.Create(request.EventDateTime),
                    request.Reason,
                    request.DisplayName,
                    CreateUserDisplayPreferences(request),
                    CreateUserValuationPreferences(request)),
                cancellationToken));

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
    }

    private static UserDisplayPreferences CreateUserDisplayPreferences(UserEventRequest request) =>
        new(request.DisplayPreferences.DarkMode, request.DisplayPreferences.RememberTraceDate);

    private static UserValuationPreferences CreateUserValuationPreferences(UserEventRequest request) =>
        new(
            EventDateTimeBuilder.Create(request.ValuationPreferences.ValuationDate),
            request.ValuationPreferences.ShowIncome,
            request.ValuationPreferences.ShowBook);

    private static async Task<IReadOnlyList<string>> ValidateInstrumentValueEvent(InstrumentID instrumentID, EventDateTime eventDateTime, AuditDateTime auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken)
    {
        var instrumentEvents = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId, cancellationToken);
        var instruments = new Instruments(eventDateTime, auditDateTime, instrumentEvents.ToList());
        var instrument = instruments.Items.SingleOrDefault(item => item.InstrumentID == instrumentID);
        if (instrument is null)
            return [$"No matching Instrument found for InstrumentID '{instrumentID}'."];

        return instrument.CFI.IsEquity
            ? []
            : [$"Instrument '{instrumentID}' has CFI '{instrument.CFI.Value}'. Only equity instruments support v1 price and income events."];
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

    private static object ToCountryEventResponse(ICountryEvent @event) =>
        @event switch
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
        };

    private static object ToCurrencyEventResponse(ICurrencyEvent @event) =>
        @event switch
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
        };

    private static object ToFXEventResponse(IFXEvent @event) =>
        @event switch
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
        };

    private static object ToFXRateEventResponse(IFXRateEvent @event) =>
        @event switch
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
        };

    private static object ToInstrumentEventResponse(IInstrumentEvent @event) =>
        @event switch
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
                PriceCountry = createdEvent.PriceCountry.Value
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
                PriceCountry = modifiedEvent.PriceCountry.Value
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
        };

    private static object ToInstrumentPriceEventResponse(IInstrumentPriceEvent @event) =>
        @event switch
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
        };

    private static object ToInstrumentIncomeEventResponse(IInstrumentIncomeEvent @event) =>
        @event switch
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
        };
}
