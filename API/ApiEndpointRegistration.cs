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

    private const string AccountEventsRoute = "/API/Events/Account";
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

    public static WebApplication MapFolioTraceApi(this WebApplication app)
    {
        var api = app.MapGroup("");

        api.MapGet("/HelloWorld", () => "Hello World!");
        api.MapDiagnosticsEndpoints();
        api.MapNotificationEndpoints();
        api.MapSystemEndpoints();
        api.MapAccountEndpoints();
        api.MapCountryEndpoints();
        api.MapCurrencyEndpoints();
        api.MapFXEndpoints();
        api.MapFXRateEndpoints();
        api.MapHoldingEndpoints();
        api.MapInstrumentEndpoints();
        api.MapInstrumentValueEndpoints();
        api.MapTicketEndpoints();
        api.MapUserMenuPreferencesEndpoints();
        api.MapUserValuationPreferencesEndpoints();
        api.MapAccountEventEndpoints();
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

        diagnostics.MapGet("/Memory", (IEventRepository eventRepository, AccountService accountService, CountryService countryService, CurrencyService currencyService, FXService fxService, FXRateService fxRateService, HoldingService holdingService, HoldingPositionService holdingPositionService, InstrumentService instrumentService, InstrumentValueService instrumentValueService, AggregateUpdateNotificationService notificationService, AggregateMaintenanceCoordinator aggregateMaintenanceCoordinator) =>
        {
            var repositoryDiagnostics = eventRepository.GetCacheDiagnostics();
            var accountDiagnostics = accountService.GetDiagnostics();
            var countryDiagnostics = countryService.GetDiagnostics();
            var currencyDiagnostics = currencyService.GetDiagnostics();
            var fxDiagnostics = fxService.GetDiagnostics();
            var fxRateDiagnostics = fxRateService.GetDiagnostics();
            var holdingDiagnostics = holdingService.GetDiagnostics();
            var holdingPositionDiagnostics = holdingPositionService.GetDiagnostics();
            var instrumentDiagnostics = instrumentService.GetDiagnostics();
            var instrumentValueDiagnostics = instrumentValueService.GetDiagnostics();
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
    }

    private static void MapSystemEndpoints(this RouteGroupBuilder api)
    {
        var system = api.MapGroup("/System");

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
        var accounts = api.MapGroup("/Accounts");

        accounts.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, AccountService accountService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await accountService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await accountService.Get(valuationDate));
        });
    }

    private static void MapHoldingEndpoints(this RouteGroupBuilder api)
    {
        var holdings = api.MapGroup("/Holdings");

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

        api.MapGet("/HoldingPositions", async (DateTime eventDateTime, DateTime? auditDateTime, ValuationDateBasis? valuationDateBasis, Guid? holdingID, Guid? accountID, Guid? instrumentID, bool? includeExcluded, bool? includeZero, HoldingPositionService holdingPositionService) =>
        {
            var valuationDateTime = EventDateTimeBuilder.Create(eventDateTime);
            var basis = valuationDateBasis ?? ValuationDateBasis.EventDateTime;
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

    private static void MapTicketEndpoints(this RouteGroupBuilder api)
    {
        var tickets = api.MapGroup("/Tickets");

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

    private static void MapUserMenuPreferencesEndpoints(this RouteGroupBuilder api)
    {
        var userMenuPreferences = api.MapGroup("/UserMenuPreferences");

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
        var userValuationPreferences = api.MapGroup("/UserValuationPreferences");

        userValuationPreferences.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, Guid? userID, UserValuationPreferencesService userValuationPreferencesService) =>
        {
            var resolvedUserID = new UserID(userID ?? Constants.Initialisation.UserID.Value);
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return auditDateTime.HasValue
                ? Results.Ok(await userValuationPreferencesService.Get(resolvedUserID, valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
                : Results.Ok(await userValuationPreferencesService.Get(resolvedUserID, valuationDate));
        });
    }

    private static void MapAccountEventEndpoints(this RouteGroupBuilder api)
    {
        var accountEvents = api.MapGroup("/Events/Account");

        accountEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IAccountEvent>(Constants.Initialisation.AccountsStreamId, cancellationToken);
            return Results.Ok(events.Select(ToAccountEventResponse).ToList());
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

        accountEvents.MapPost($"/{nameof(AccountActiveModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AccountActiveModifiedRequest request, CancellationToken cancellationToken) =>
        {
            var accounts = await TryGetAccounts(request.EventDateTime, AuditDateTimeBuilder.Create(), eventRepository, cancellationToken);
            if (accounts is null)
                return Results.BadRequest(Result<AccountActiveModifiedEvent>.Invalid([$"No matching Account found for AccountID '{request.AccountID}'."]));

            var result = AccountActiveModifiedEventBuilder.Create(request, accounts);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            await eventRepository.AppendAsync(Constants.Initialisation.AccountsStreamId, result.Value, cancellationToken);
            cacheInvalidationService.Invalidate(result.Value);
            return Results.Accepted(AccountEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(AccountEventsRoute, result.Value));
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

    private static void MapHoldingEventEndpoints(this RouteGroupBuilder api)
    {
        var holdingEvents = api.MapGroup("/Events/Holding");

        holdingEvents.MapGet("/", async (Guid? holdingID, Guid? accountID, Guid? instrumentID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
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

            return Results.Ok(events.Select(ToHoldingEventResponse).ToList());
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
        MapHoldingCreatedEndpoint<HoldingCashDebtCreatedRequest, HoldingCashDebtCreatedEvent>(holdingEvents, nameof(HoldingCashDebtCreatedEvent), HoldingCashDebtCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingCashInvestableCreatedRequest, HoldingCashInvestableCreatedEvent>(holdingEvents, nameof(HoldingCashInvestableCreatedEvent), HoldingCashInvestableCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingCashNonInvestableCreatedRequest, HoldingCashNonInvestableCreatedEvent>(holdingEvents, nameof(HoldingCashNonInvestableCreatedEvent), HoldingCashNonInvestableCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingInflowCreatedRequest, HoldingInflowCreatedEvent>(holdingEvents, nameof(HoldingInflowCreatedEvent), HoldingInflowCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingOutflowCreatedRequest, HoldingOutflowCreatedEvent>(holdingEvents, nameof(HoldingOutflowCreatedEvent), HoldingOutflowCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingInspecieInCreatedRequest, HoldingInspecieInCreatedEvent>(holdingEvents, nameof(HoldingInspecieInCreatedEvent), HoldingInspecieInCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingInspecieOutCreatedRequest, HoldingInspecieOutCreatedEvent>(holdingEvents, nameof(HoldingInspecieOutCreatedEvent), HoldingInspecieOutCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingFeesCustodianCreatedRequest, HoldingFeesCustodianCreatedEvent>(holdingEvents, nameof(HoldingFeesCustodianCreatedEvent), HoldingFeesCustodianCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingFeesAdministratorCreatedRequest, HoldingFeesAdministratorCreatedEvent>(holdingEvents, nameof(HoldingFeesAdministratorCreatedEvent), HoldingFeesAdministratorCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingFeesBankCreatedRequest, HoldingFeesBankCreatedEvent>(holdingEvents, nameof(HoldingFeesBankCreatedEvent), HoldingFeesBankCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingIncomeCreatedRequest, HoldingIncomeCreatedEvent>(holdingEvents, nameof(HoldingIncomeCreatedEvent), HoldingIncomeCreatedEventBuilder.Create);
        MapHoldingCreatedEndpoint<HoldingInterestCreatedRequest, HoldingInterestCreatedEvent>(holdingEvents, nameof(HoldingInterestCreatedEvent), HoldingInterestCreatedEventBuilder.Create);

        MapHoldingModifiedEndpoint<HoldingPositionMemoModifiedRequest, HoldingPositionMemoModifiedEvent>(holdingEvents, nameof(HoldingPositionMemoModifiedEvent), HoldingPositionMemoModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingPositionCashModifiedRequest, HoldingPositionCashModifiedEvent>(holdingEvents, nameof(HoldingPositionCashModifiedEvent), HoldingPositionCashModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingCashDebtModifiedRequest, HoldingCashDebtModifiedEvent>(holdingEvents, nameof(HoldingCashDebtModifiedEvent), HoldingCashDebtModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingCashInvestableModifiedRequest, HoldingCashInvestableModifiedEvent>(holdingEvents, nameof(HoldingCashInvestableModifiedEvent), HoldingCashInvestableModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingCashNonInvestableModifiedRequest, HoldingCashNonInvestableModifiedEvent>(holdingEvents, nameof(HoldingCashNonInvestableModifiedEvent), HoldingCashNonInvestableModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingInflowModifiedRequest, HoldingInflowModifiedEvent>(holdingEvents, nameof(HoldingInflowModifiedEvent), HoldingInflowModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingOutflowModifiedRequest, HoldingOutflowModifiedEvent>(holdingEvents, nameof(HoldingOutflowModifiedEvent), HoldingOutflowModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingInspecieInModifiedRequest, HoldingInspecieInModifiedEvent>(holdingEvents, nameof(HoldingInspecieInModifiedEvent), HoldingInspecieInModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingInspecieOutModifiedRequest, HoldingInspecieOutModifiedEvent>(holdingEvents, nameof(HoldingInspecieOutModifiedEvent), HoldingInspecieOutModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingFeesCustodianModifiedRequest, HoldingFeesCustodianModifiedEvent>(holdingEvents, nameof(HoldingFeesCustodianModifiedEvent), HoldingFeesCustodianModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingFeesAdministratorModifiedRequest, HoldingFeesAdministratorModifiedEvent>(holdingEvents, nameof(HoldingFeesAdministratorModifiedEvent), HoldingFeesAdministratorModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingFeesBankModifiedRequest, HoldingFeesBankModifiedEvent>(holdingEvents, nameof(HoldingFeesBankModifiedEvent), HoldingFeesBankModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingIncomeModifiedRequest, HoldingIncomeModifiedEvent>(holdingEvents, nameof(HoldingIncomeModifiedEvent), HoldingIncomeModifiedEventBuilder.Create);
        MapHoldingModifiedEndpoint<HoldingInterestModifiedRequest, HoldingInterestModifiedEvent>(holdingEvents, nameof(HoldingInterestModifiedEvent), HoldingInterestModifiedEventBuilder.Create);

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

        priceEvents.MapGet("/", async (Guid? instrumentID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IInstrumentPriceEvent>(Constants.Initialisation.InstrumentPricesStreamId, cancellationToken);
            if (instrumentID.HasValue)
                events = events
                    .Where(@event => @event is InstrumentPriceSetEvent setEvent && setEvent.InstrumentID.Value == instrumentID.Value)
                    .ToList();

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

        incomeEvents.MapGet("/", async (Guid? instrumentID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IInstrumentIncomeEvent>(Constants.Initialisation.InstrumentIncomesStreamId, cancellationToken);
            if (instrumentID.HasValue)
                events = events
                    .Where(@event => @event is InstrumentIncomeSetEvent setEvent && setEvent.InstrumentID.Value == instrumentID.Value)
                    .ToList();

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

    private static void MapTransactionEventEndpoints(this RouteGroupBuilder api)
    {
        var transactionEvents = api.MapGroup("/Events/Transaction");

        transactionEvents.MapGet("/", async (Guid? eventSetID, Guid? holdingID, Guid? accountID, Guid? instrumentID, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId, cancellationToken);

            if (eventSetID.HasValue)
                events = events
                    .Where(@event => @event switch
                    {
                        ITransactionMovementEvent movementEvent => movementEvent.EventSetID.Value == eventSetID.Value,
                        TransactionCancellationEvent cancellationEvent => cancellationEvent.EventSetID.Value == eventSetID.Value,
                        _ => false
                    })
                    .ToList();

            if (holdingID.HasValue)
                events = events
                    .OfType<ITransactionMovementEvent>()
                    .Where(@event => @event.HoldingID?.Value == holdingID.Value)
                    .Cast<ITransactionEvent>()
                    .ToList();

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
                        _ => false
                    })
                    .ToList();
            }

            if (instrumentID.HasValue)
                events = events
                    .OfType<ITransactionMovementEvent>()
                    .Where(@event => @event.InstrumentID.Value == instrumentID.Value)
                    .Cast<ITransactionEvent>()
                    .ToList();

            return Results.Ok(events.Select(ToTransactionEventResponse).ToList());
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
            var holdings = await TryGetHoldings(request.EventDateTime, AuditDateTimeBuilder.Create(), eventRepository, cancellationToken);
            var result = TransactionBuilder.Create(request, holdings);
            if (!result.IsValid || result.Value is null)
                return Results.BadRequest(result);

            var events = result.Value.Cast<IEventBase>().ToList();
            await eventRepository.AppendAsync(Constants.Initialisation.TransactionsStreamId, events, cancellationToken);
            cacheInvalidationService.Invalidate(events);

            return Results.Accepted(TransactionEventsRoute, CreateAcceptedEventsResponse(TransactionEventsRoute, events));
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
        var ticketEvents = api.MapGroup("/Events/Ticket");

        ticketEvents.MapGet("/", async (int? ticketNumber, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<ITicket>(Constants.Initialisation.TicketsStreamId, cancellationToken);
            if (ticketNumber.HasValue)
                events = events.Where(@event => @event.TicketNumber.Value == ticketNumber.Value).ToList();

            return Results.Ok(events.Select(ToTicketEventResponse).ToList());
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

        ticketEvents.MapPost($"/{nameof(TicketTradeCreatedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.CreateTrade(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeModifiedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketTradeRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.ModifyTrade(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
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

        ticketEvents.MapPost($"/{nameof(TicketTradeApprovedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.ApproveTrade(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
                cancellationToken));

        ticketEvents.MapPost($"/{nameof(TicketTradeNotApprovedEvent)}", async (TicketService ticketService, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, TicketApprovalRequest request, CancellationToken cancellationToken) =>
            await AppendTicketEvent(
                eventRepository,
                cacheInvalidationService,
                async () => TicketEventBuilder.NotApproveTrade(request, await ticketService.Get(request.EventDateTime, AuditDateTimeBuilder.Create())),
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
        var userEvents = api.MapGroup("/Events/User");

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
    }

    private static void MapUserMenuPreferencesEventEndpoints(this RouteGroupBuilder api)
    {
        var userMenuPreferencesEvents = api.MapGroup("/Events/UserMenuPreferences");

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
        var userValuationPreferencesEvents = api.MapGroup("/Events/UserValuationPreferences");

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

    private static UserDisplayPreferences CreateUserDisplayPreferences(UserEventRequest request) =>
        new(request.DisplayPreferences.DarkMode, request.DisplayPreferences.RememberTraceDate);

    private static UserProfileValuationPreferences CreateUserValuationPreferences(UserEventRequest request) =>
        new(
            EventDateTimeBuilder.Create(request.ValuationPreferences.ValuationDate),
            request.ValuationPreferences.ShowIncome,
            request.ValuationPreferences.ShowBook);

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
        @event switch
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
            AccountActiveModifiedEvent activeEvent => new
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

    private static object ToHoldingEventResponse(IHoldingEvent @event) =>
        @event switch
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
                BankName = createdEvent is HoldingBankCreatedEvent bankEvent ? bankEvent.BankName : null,
                AccountName = createdEvent is HoldingBankCreatedEvent accountEvent ? accountEvent.AccountName : null,
                SortCode = createdEvent is HoldingBankCreatedEvent sortEvent ? sortEvent.SortCode.Value : null,
                AccountNumber = createdEvent is HoldingBankCreatedEvent numberEvent ? numberEvent.AccountNumber.Value : null,
                BIC = createdEvent is HoldingBankCreatedEvent bicEvent ? bicEvent.BIC.Value : null,
                IBAN = createdEvent is HoldingBankCreatedEvent ibanEvent ? ibanEvent.IBAN.Value : null
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
                BankName = modifiedEvent is HoldingBankModifiedEvent bankEvent ? bankEvent.BankName : null,
                AccountName = modifiedEvent is HoldingBankModifiedEvent accountEvent ? accountEvent.AccountName : null,
                SortCode = modifiedEvent is HoldingBankModifiedEvent sortEvent ? sortEvent.SortCode.Value : null,
                AccountNumber = modifiedEvent is HoldingBankModifiedEvent numberEvent ? numberEvent.AccountNumber.Value : null,
                BIC = modifiedEvent is HoldingBankModifiedEvent bicEvent ? bicEvent.BIC.Value : null,
                IBAN = modifiedEvent is HoldingBankModifiedEvent ibanEvent ? ibanEvent.IBAN.Value : null
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

    private static object ToTransactionEventResponse(ITransactionEvent @event) =>
        @event switch
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
                BookCost = creditEvent.BookCost.Value
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
                BookCost = debitEvent.BookCost.Value
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
        };

    private static object ToTicketEventResponse(ITicket @event) =>
        @event switch
        {
            TicketCreatedEvent createdEvent => WithTicketBase(createdEvent, new
            {
                Side = createdEvent.Side.ToString(),
                InstrumentID = createdEvent.InstrumentID.Value
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
                proposalCreatedEvent.TotalAmount,
                Allocations = proposalCreatedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketProposalModifiedEvent proposalModifiedEvent => WithTicketBase(proposalModifiedEvent, new
            {
                proposalModifiedEvent.TargetPrice,
                proposalModifiedEvent.TotalAmount,
                Allocations = proposalModifiedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketTradeCreatedEvent tradeCreatedEvent => WithTicketBase(tradeCreatedEvent, new
            {
                tradeCreatedEvent.TradedPrice,
                Allocations = tradeCreatedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketTradeModifiedEvent tradeModifiedEvent => WithTicketBase(tradeModifiedEvent, new
            {
                tradeModifiedEvent.TradedPrice,
                Allocations = tradeModifiedEvent.Allocations.Select(ToResponse).ToList()
            }),
            TicketTradeFillAddedEvent fillAddedEvent => WithTicketBase(fillAddedEvent, new
            {
                fillAddedEvent.FillID,
                fillAddedEvent.Price,
                fillAddedEvent.Quantity,
                fillAddedEvent.Note
            }),
            TicketTradeFillModifiedEvent fillModifiedEvent => WithTicketBase(fillModifiedEvent, new
            {
                fillModifiedEvent.FillID,
                fillModifiedEvent.Price,
                fillModifiedEvent.Quantity,
                fillModifiedEvent.Note
            }),
            TicketTradeFillRemovedEvent fillRemovedEvent => WithTicketBase(fillRemovedEvent, new
            {
                fillRemovedEvent.FillID
            }),
            _ => WithTicketBase(@event, new { })
        };

    private static object WithTicketBase(ITicket @event, object details) =>
        new
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
            allocation.Quantity,
            allocation.BookCost
        };
}
