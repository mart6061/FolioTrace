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
    private static void MapInputControlSettingsEventEndpoints(this RouteGroupBuilder api)
    {
        var inputControlSettingsEvents = api.MapGroup("/Events/InputControlSettings").WithTags("Input Control Settings Events");

        inputControlSettingsEvents.MapGet("/", async (DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var events = await eventRepository.LoadStreamAsync<IInputControlSettingsEvent>(Constants.Initialisation.InputControlSettingsStreamId, cancellationToken);

            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, ToInputControlSettingsEventResponse));
        });

        inputControlSettingsEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
        {
            var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
            return @event is IInputControlSettingsEvent inputControlSettingsEvent
                ? Results.Ok(ToInputControlSettingsEventResponse(inputControlSettingsEvent))
                : Results.NotFound();
        });

        inputControlSettingsEvents.MapPost($"/{nameof(InputControlSettingsCreatedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, InputControlSettingsRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.InputControlSettingsStreamId,
                InputControlSettingsEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => InputControlSettingsCreatedEventBuilder.Create(request),
                cancellationToken));

        inputControlSettingsEvents.MapPost($"/{nameof(InputControlSettingsModifiedEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, InputControlSettingsRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.InputControlSettingsStreamId,
                InputControlSettingsEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => InputControlSettingsModifiedEventBuilder.Create(request),
                cancellationToken));
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

        accountEvents.MapPost($"/{nameof(AccountIdentifierSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AccountIdentifierSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(Constants.Initialisation.AccountsStreamId, AccountEventsRoute, eventRepository, cacheInvalidationService, () => AccountIdentifierSetEventBuilder.Create(request), cancellationToken));

        accountEvents.MapPost($"/{nameof(AccountIdentifierUnsetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, AccountIdentifierUnsetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(Constants.Initialisation.AccountsStreamId, AccountEventsRoute, eventRepository, cacheInvalidationService, () => AccountIdentifierUnsetEventBuilder.Create(request), cancellationToken));
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
        {
            var createdResult = BrokerCreatedEventBuilder.Create(request);
            if (!createdResult.IsValid || createdResult.Value is null)
                return Results.BadRequest(createdResult);
            var manualResult = BrokerTradeMethodEventBuilder.Set(new BrokerTradeMethodSetRequest(
                request.UserID, request.EventDateTime, $"Enable manual trading for broker {request.LEI}", request.LEI, new ManualTradeMethod()));
            if (!manualResult.IsValid || manualResult.Value is null)
                return Results.BadRequest(manualResult);
            await eventRepository.AppendAsync(Constants.Initialisation.BrokersStreamId, [(IAuditEventBase)createdResult.Value, (IAuditEventBase)manualResult.Value], cancellationToken);
            cacheInvalidationService.Invalidate([createdResult.Value, manualResult.Value]);
            return Results.Accepted(BrokerEventsRoute, EventEndpointFactory.CreateAcceptedEventResponse(BrokerEventsRoute, createdResult.Value));
        });

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

        brokerEvents.MapPost("/TradeMethodSetEvent", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, BrokerTradeMethodSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(Constants.Initialisation.BrokersStreamId, BrokerEventsRoute, eventRepository, cacheInvalidationService, () => BrokerTradeMethodEventBuilder.Set(request), cancellationToken));

        brokerEvents.MapPost($"/{nameof(BrokerTradeMethodUnsetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, BrokerTradeMethodUnsetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(Constants.Initialisation.BrokersStreamId, BrokerEventsRoute, eventRepository, cacheInvalidationService, () => BrokerTradeMethodEventBuilder.Unset(request), cancellationToken));
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

        fxRateEvents.MapPost($"/{nameof(FXRateSetEvent)}", async (IEventRepository eventRepository, AggregateCacheInvalidationService cacheInvalidationService, FXRateSetRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(
                Constants.Initialisation.FXRatesStreamId,
                FXRateEventsRoute,
                eventRepository,
                cacheInvalidationService,
                () => FXRateSetEventBuilder.Create(request),
                cancellationToken));
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

}
