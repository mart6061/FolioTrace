using System.Diagnostics;
using System.Reflection;
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
    private const string FXEventsRoute = "/API/Events/FX";
    private const string FXRateEventsRoute = "/API/Events/FXRate";
    private const string UserEventsRoute = "/API/Events/User";

    public static WebApplication MapFolioTraceApi(this WebApplication app)
    {
        var api = app.MapGroup("");

        api.MapGet("/HelloWorld", () => "Hello World!");
        api.MapDiagnosticsEndpoints();
        api.MapSystemEndpoints();
        api.MapCountryEndpoints();
        api.MapCurrencyEndpoints();
        api.MapFXEndpoints();
        api.MapFXRateEndpoints();
        api.MapCountryEventEndpoints();
        api.MapCurrencyEventEndpoints();
        api.MapFXEventEndpoints();
        api.MapFXRateEventEndpoints();
        api.MapUserEventEndpoints();

        return app;
    }

    private static void MapDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        var diagnostics = api.MapGroup("/Diagnostics");

        diagnostics.MapGet("/Memory", (IEventRepository eventRepository, CountryService countryService, CurrencyService currencyService, FXService fxService, FXRateService fxRateService) =>
        {
            var repositoryDiagnostics = eventRepository.GetCacheDiagnostics();
            var countryDiagnostics = countryService.GetDiagnostics();
            var currencyDiagnostics = currencyService.GetDiagnostics();
            var fxDiagnostics = fxService.GetDiagnostics();
            var fxRateDiagnostics = fxRateService.GetDiagnostics();

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
                    fxRateDiagnostics.FXRateCount)));
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

        system.MapGet("/Version", () =>
        {
            return Results.Ok(new
            {
                ApiVersion = CreateDisplayVersion(typeof(ApiEndpointRegistration).Assembly)
            });
        });

        system.MapPost("/Build", async (
            ISeedRepository seedRepository,
            CountryService countryService,
            CurrencyService currencyService,
            FXService fxService,
            FXRateService fxRateService,
            CancellationToken cancellationToken) =>
        {
            await seedRepository.Build(cancellationToken);

            var removedCountryViews = countryService.InvalidateAll();
            var removedCurrencyViews = currencyService.InvalidateAll();
            var removedFXViews = fxService.InvalidateAll();
            var removedFXRateViews = fxRateService.InvalidateAll();

            return Results.Ok(new
            {
                Status = "Complete",
                Message = "Database rebuild complete.",
                RemovedCacheViews = new
                {
                    Countries = removedCountryViews,
                    Currencies = removedCurrencyViews,
                    FXs = removedFXViews,
                    FXRates = removedFXRateViews
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

        fxEvents.MapPost($"/{nameof(FXCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, FXCreatedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.FXsStreamId,
                FXEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => FXCreatedEventBuilder.Create(request),
                cancellationToken));

        fxEvents.MapPost($"/{nameof(FXActiveModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, FXActiveModifiedRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.FXsStreamId,
                FXEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => FXActiveModifiedEventBuilder.Create(request),
                cancellationToken));
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

        fxRateEvents.MapPost($"/{nameof(FXRateSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, FXRateSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.FXRatesStreamId,
                FXRateEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => FXRateSetEventBuilder.Create(request),
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

    private static string CreateDisplayVersion(Assembly assembly)
    {
        var baseVersion = GetBaseVersion(assembly);
        var buildNumber = TryRunGit("rev-list", "--count", "HEAD");
        var revisionHash = TryRunGit("rev-parse", "--short=4", "HEAD");
        var build = int.TryParse(buildNumber, out var parsedBuild)
            ? parsedBuild
            : baseVersion.Build < 0 ? 0 : baseVersion.Build;
        var revision = TryParseHexRevision(revisionHash, out var parsedRevision)
            ? parsedRevision
            : baseVersion.Revision < 0 ? 0 : baseVersion.Revision;

        return $"{baseVersion.Major}.{baseVersion.Minor}.{build}.{revision}";
    }

    private static Version GetBaseVersion(Assembly assembly)
    {
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var versionText = informationalVersion?.Split('+')[0].Split('-')[0];

        return Version.TryParse(versionText, out var version)
            ? version
            : assembly.GetName().Version ?? new Version(0, 0, 0, 0);
    }

    private static bool TryParseHexRevision(string? value, out int revision)
    {
        revision = 0;
        return !string.IsNullOrWhiteSpace(value)
            && int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out revision);
    }

    private static string? TryRunGit(params string[] arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = string.Join(' ', arguments),
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = Directory.GetCurrentDirectory()
            });

            if (process is null)
                return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            if (!process.WaitForExit(1_000))
            {
                process.Kill(true);
                return null;
            }

            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : null;
        }
        catch
        {
            return null;
        }
    }

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
}
