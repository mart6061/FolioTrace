using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace API;

public static class ApiEndpointRegistration
{
    private const string CountryEventsRoute = "/API/Events/Country";
    private const string CurrencyEventsRoute = "/API/Events/Currency";
    private const string UserEventsRoute = "/API/Events/User";

    public static WebApplication MapFolioTraceApi(this WebApplication app)
    {
        var api = app.MapGroup("");

        api.MapGet("/HelloWorld", () => "Hello World!");
        api.MapDiagnosticsEndpoints();
        api.MapCountryEndpoints();
        api.MapCountryEventEndpoints();
        api.MapCurrencyEventEndpoints();
        api.MapUserEventEndpoints();

        return app;
    }

    private static void MapDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        var diagnostics = api.MapGroup("/Diagnostics");

        diagnostics.MapGet("/Memory", (IEventRepository eventRepository, CountryService countryService) =>
        {
            var repositoryDiagnostics = eventRepository.GetCacheDiagnostics();
            var countryDiagnostics = countryService.GetDiagnostics();

            return Results.Ok(new MemoryDiagnosticsResponse(
                new EventCacheDiagnosticsResponse(
                    repositoryDiagnostics.IsLoaded,
                    repositoryDiagnostics.StreamCount,
                    repositoryDiagnostics.EventCount),
                new CountryServiceDiagnosticsResponse(
                    countryDiagnostics.CacheEntryCount,
                    countryDiagnostics.CountryCount)));
        });

        diagnostics.MapGet("/HttpExchanges", async (
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

        diagnostics.MapGet("/HttpExchanges/{id:guid}", async (Guid id, IApiExchangeRepository repository, CancellationToken cancellationToken) =>
        {
            var exchange = await repository.LoadAsync(id, cancellationToken);

            return exchange is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(exchange));
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

    private static void MapCountryEventEndpoints(this RouteGroupBuilder api)
    {
        var countryEvents = api.MapGroup("/Events/Country");

        countryEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
            await eventRepository.LoadStreamAsync<ICountryEvent>(Constants.Initialisation.CountriesStreamId, cancellationToken));

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

        currencyEvents.MapPost($"/{nameof(CurrencyCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CurrencyEventRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.CurrenciesStreamId,
                CurrencyEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () =>
                CurrencyCreatedEventBuilder.Create(
                    request.UserID,
                    EventDateTimeBuilder.Create(request.EventDateTime),
                    request.Reason,
                    Alpha3Builder.Create(request.AlphabeticCode),
                    request.NumericCode,
                    request.DecimalPlace,
                    request.Name),
                cancellationToken));

        currencyEvents.MapPost($"/{nameof(CurrencyModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CurrencyEventRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.CurrenciesStreamId,
                CurrencyEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () =>
                CurrencyModifiedEventBuilder.Create(
                    request.UserID,
                    EventDateTimeBuilder.Create(request.EventDateTime),
                    request.Reason,
                    Alpha3Builder.Create(request.AlphabeticCode),
                    request.NumericCode,
                    request.DecimalPlace,
                    request.Name),
                cancellationToken));
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
}
