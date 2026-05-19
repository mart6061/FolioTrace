using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Scalar.AspNetCore;
using Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddFolioTraceRepository(builder.Configuration);
builder.Services.AddFolioTraceServices();

var app = builder.Build();

app.UsePathBase("/API");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

var api = app.MapGroup("");
var countries = api.MapGroup("/Countries");
var countryEvents = api.MapGroup("/Events/Country");
var currencyEvents = api.MapGroup("/Events/Currency");
var userEvents = api.MapGroup("/Events/User");
var diagnostics = api.MapGroup("/Diagnostics");

var helloWorld = api.MapGet("/HelloWorld", () => "Hello World!");

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

countries.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, CountryService countryService) =>
{
    var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

    return auditDateTime.HasValue
        ? Results.Ok(await countryService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
        : Results.Ok(await countryService.Get(valuationDate));
});

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
        "/API/Events/Country",
        eventData.Select(@event => new
        {
            EventID = @event.EventID.Value,
            Links = new[]
            {
                new
                {
                    Rel = "self",
                    Href = $"/API/Events/Country/{@event.EventID.Value}",
                    Method = "GET"
                }
            }
        }));
});

countryEvents.MapPost($"/{nameof(CountryCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CountryCreatedRequest request, CancellationToken cancellationToken) =>
    await EventEndpointFactory.CreateAndAppend(
        Constants.Initialisation.CountriesStreamId,
        "/API/Events/Country",
        eventRepository,
        cacheInvalidationService,
        () => CountryCreatedEventBuilder.Create(request),
        cancellationToken));

countryEvents.MapPost($"/{nameof(CountryModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CountryModifiedRequest request, CancellationToken cancellationToken) =>
    await EventEndpointFactory.CreateAndAppend(
        Constants.Initialisation.CountriesStreamId,
        "/API/Events/Country",
        eventRepository,
        cacheInvalidationService,
        () => CountryModifiedEventBuilder.Create(request),
        cancellationToken));

countryEvents.MapPost($"/{nameof(CountryFlagModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CountryFlagModifiedRequest request, CancellationToken cancellationToken) =>
    await EventEndpointFactory.CreateAndAppend(
        Constants.Initialisation.CountriesStreamId,
        "/API/Events/Country",
        eventRepository,
        cacheInvalidationService,
        () => CountryFlagModifiedEventBuilder.Create(request),
        cancellationToken));

currencyEvents.MapPost($"/{nameof(CurrencyCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, CurrencyEventRequest request, CancellationToken cancellationToken) =>
    await EventEndpointFactory.CreateAndAppend(
        Constants.Initialisation.CurrenciesStreamId,
        "/API/Events/Currency",
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
        "/API/Events/Currency",
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

userEvents.MapPost($"/{nameof(UserCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserEventRequest request, CancellationToken cancellationToken) =>
    await EventEndpointFactory.CreateAndAppend(
        Constants.Initialisation.UsersStreamId,
        "/API/Events/User",
        eventRepository,
        cacheInvalidationService,
        () =>
        UserCreatedEventBuilder.Create(
            request.UserID,
            EventDateTimeBuilder.Create(request.EventDateTime),
            request.Reason,
            request.DisplayName,
            new UserDisplayPreferences(request.DisplayPreferences.DarkMode, request.DisplayPreferences.RememberTraceDate),
            new UserValuationPreferences(EventDateTimeBuilder.Create(request.ValuationPreferences.ValuationDate), request.ValuationPreferences.ShowIncome, request.ValuationPreferences.ShowBook)),
        cancellationToken));

userEvents.MapPost($"/{nameof(UserModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, UserEventRequest request, CancellationToken cancellationToken) =>
    await EventEndpointFactory.CreateAndAppend(
        Constants.Initialisation.UsersStreamId,
        "/API/Events/User",
        eventRepository,
        cacheInvalidationService,
        () =>
        UserModifiedEventBuilder.Create(
            request.UserID,
            EventDateTimeBuilder.Create(request.EventDateTime),
            request.Reason,
            request.DisplayName,
            new UserDisplayPreferences(request.DisplayPreferences.DarkMode, request.DisplayPreferences.RememberTraceDate),
            new UserValuationPreferences(EventDateTimeBuilder.Create(request.ValuationPreferences.ValuationDate), request.ValuationPreferences.ShowIncome, request.ValuationPreferences.ShowBook)),
        cancellationToken));

app.Run();

public sealed record CurrencyEventRequest(Guid UserID, DateTime EventDateTime, string Reason, string AlphabeticCode, int NumericCode, short DecimalPlace, string Name);

public sealed record UserEventRequest(Guid UserID, DateTime EventDateTime, string Reason, string DisplayName, UserDisplayPreferencesRequest DisplayPreferences, UserValuationPreferencesRequest ValuationPreferences);

public sealed record UserDisplayPreferencesRequest(bool DarkMode, bool RememberTraceDate);

public sealed record UserValuationPreferencesRequest(DateTime ValuationDate, bool ShowIncome, bool ShowBook);

public sealed record MemoryDiagnosticsResponse(EventCacheDiagnosticsResponse EventCache, CountryServiceDiagnosticsResponse CountryService);

public sealed record EventCacheDiagnosticsResponse(bool IsLoaded, int StreamCount, int EventCount);

public sealed record CountryServiceDiagnosticsResponse(int CacheEntryCount, int CountryCount);

public static class EventEndpointFactory
{
    public static async Task<IResult> CreateAndAppend<TEvent>(Guid streamId, string eventRoute, IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, Func<Result<TEvent>> createEvent, CancellationToken cancellationToken)
        where TEvent : class, IEventBase
    {
        var result = Create(createEvent);
        if (!result.IsValid || result.Value is null)
            return Results.BadRequest(result);

        await eventRepository.AppendAsync(streamId, result.Value, cancellationToken);
        cacheInvalidationService.Invalidate(result.Value);

        return Results.Accepted(
            eventRoute,
            new
        {
            EventID = result.Value.EventID.Value,
            Links = new[]
            {
                new
                {
                    Rel = "self",
                    Href = $"{eventRoute}/{result.Value.EventID.Value}",
                    Method = "GET"
                }
            }
        });
    }

    private static Result<TEvent> Create<TEvent>(Func<Result<TEvent>> createEvent) =>
        TryCreate(createEvent);

    private static Result<TEvent> TryCreate<TEvent>(Func<Result<TEvent>> createEvent)
    {
        try
        {
            return createEvent();
        }
        catch (ArgumentException exception)
        {
            return Result<TEvent>.Invalid([exception.Message]);
        }
        catch (FormatException exception)
        {
            return Result<TEvent>.Invalid([exception.Message]);
        }
        catch (NullReferenceException exception)
        {
            return Result<TEvent>.Invalid([exception.Message]);
        }
    }
}
