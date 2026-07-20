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
    private static void MapAccountEndpoints(this RouteGroupBuilder api)
    {
        var accounts = api.MapGroup("/Accounts").WithTags("Accounts");

        accounts.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, AccountService accountService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => accountService.Get(valuationDate),
                asAt => accountService.Get(valuationDate, asAt)));
        });
    }

    private static void MapBrokerEndpoints(this RouteGroupBuilder api)
    {
        var brokers = api.MapGroup("/Brokers").WithTags("Brokers");

        brokers.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, BrokerService brokerService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => brokerService.Get(valuationDate),
                asAt => brokerService.Get(valuationDate, asAt)));
        });
    }

    private static void MapHoldingEndpoints(this RouteGroupBuilder api)
    {
        var holdings = api.MapGroup("/Holdings").WithTags("Holdings");

        holdings.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, Guid? holdingID, Guid? accountID, Guid? instrumentID, string? holdingKind, bool? includeInactive, HoldingService holdingService) =>
        {
            var valuationDateTime = EventDateTimeBuilder.Create(eventDateTime);
            var aggregate = await GetAsAt(auditDateTime,
                () => holdingService.Get(valuationDateTime),
                asAt => holdingService.Get(valuationDateTime, asAt));

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
            var asAt = GetAsAt(auditDateTime);
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
            var asAt = GetAsAt(auditDateTime);
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

    private static void MapSystemHealthEndpoint(this RouteGroupBuilder api)
    {
        var system = api.MapGroup("/System").WithTags("System").AllowAnonymous();

        system.MapGet("/Health", (ApiReadinessState readinessState, FixStartupHealthState fixHealthState) =>
        {
            var fix = fixHealthState.Snapshot;
            var status = !readinessState.Ready || fix.Status == FixStartupStatus.Starting
                ? "Starting"
                : fix.Status == FixStartupStatus.Failed ? "Degraded" : "Ready";

            return Results.Ok(new
            {
                Ready = readinessState.Ready,
                Status = status,
                Service = "FolioTrace API",
                Fix = new
                {
                    Status = fix.Status.ToString(),
                    fix.FailureMessage,
                    fix.FailedAtUtc
                },
                CheckedAtUtc = DateTime.UtcNow
            });
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
            var asAt = GetAsAt(auditDateTime);
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

            return Results.Ok(await GetAsAt(auditDateTime,
                () => valuationSettingService.Get(valuationDate),
                asAt => valuationSettingService.Get(valuationDate, asAt)));
        });
    }

    private static void MapAssetAllocationMappingEndpoints(this RouteGroupBuilder api)
    {
        var mappings = api.MapGroup("/AssetAllocationMappings").WithTags("Asset Allocation Mappings");

        mappings.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, Guid? assetAllocationID, Guid? accountID, Guid? holdingID, AssetAllocationMappingService assetAllocationMappingService, HoldingService holdingService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var asAt = GetAsAt(auditDateTime);
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

            return Results.Ok(await GetAsAt(auditDateTime,
                () => reportConfigService.Get(valuationDate),
                asAt => reportConfigService.Get(valuationDate, asAt)));
        });
    }

    private static void MapCountryEndpoints(this RouteGroupBuilder api)
    {
        var countries = api.MapGroup("/Countries").WithTags("Countries");

        countries.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, CountryService countryService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => countryService.Get(valuationDate),
                asAt => countryService.Get(valuationDate, asAt)));
        });
    }

    private static void MapCurrencyEndpoints(this RouteGroupBuilder api)
    {
        var currencies = api.MapGroup("/Currencies").WithTags("Currencies");

        currencies.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, CurrencyService currencyService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => currencyService.Get(valuationDate),
                asAt => currencyService.Get(valuationDate, asAt)));
        });
    }

    private static void MapFXEndpoints(this RouteGroupBuilder api)
    {
        var fxs = api.MapGroup("/FXs").WithTags("FX");

        fxs.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, FXService fxService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => fxService.Get(valuationDate),
                asAt => fxService.Get(valuationDate, asAt)));
        });
    }

    private static void MapFXRateEndpoints(this RouteGroupBuilder api)
    {
        var fxRates = api.MapGroup("/FXRates").WithTags("FX Rates");

        fxRates.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, FXRateService fxRateService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => fxRateService.Get(valuationDate),
                asAt => fxRateService.Get(valuationDate, asAt)));
        });
    }

    private static void MapFoleoTraderEndpoints(this RouteGroupBuilder api)
    {
        var foleoTrader = api.MapGroup("/Trading/FoleoTrader").WithTags("FoleoTrader");

        foleoTrader.MapGet("/Orders", async (DateTime eventDateTime, DateTime? auditDateTime, FoleoTraderOrderService foleoTraderOrderService, CancellationToken cancellationToken) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => foleoTraderOrderService.Get(valuationDate, cancellationToken),
                asAt => foleoTraderOrderService.Get(valuationDate, asAt, cancellationToken)));
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

            return Results.Ok(await GetAsAt(auditDateTime,
                () => instrumentService.Get(valuationDate),
                asAt => instrumentService.Get(valuationDate, asAt)));
        });
    }

    private static void MapInstrumentValueEndpoints(this RouteGroupBuilder api)
    {
        var instrumentValues = api.MapGroup("/InstrumentValues").WithTags("Instrument Values");

        instrumentValues.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, InstrumentValueService instrumentValueService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => instrumentValueService.Get(valuationDate),
                asAt => instrumentValueService.Get(valuationDate, asAt)));
        });
    }

    private static void MapTicketEndpoints(this RouteGroupBuilder api)
    {
        var tickets = api.MapGroup("/Tickets").WithTags("Tickets");

        tickets.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, bool? includeClosed, TicketService ticketService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var aggregate = await GetAsAt(auditDateTime,
                () => ticketService.Get(valuationDate),
                asAt => ticketService.Get(valuationDate, asAt));

            var items = includeClosed == true
                ? aggregate.Items
                : aggregate.Items.Where(ticket => ticket.IsActive).ToList();

            return Results.Ok(aggregate with { Items = items });
        });

        tickets.MapGet("/Details", async (DateTime eventDateTime, DateTime? auditDateTime, bool? includeClosed, TicketService ticketService, InstrumentService instrumentService, AccountService accountService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var ticketAggregate = await GetAsAt(auditDateTime,
                () => ticketService.Get(valuationDate),
                asAt => ticketService.Get(valuationDate, asAt));
            var instruments = await GetAsAt(auditDateTime,
                () => instrumentService.Get(valuationDate),
                asAt => instrumentService.Get(valuationDate, asAt));
            var accounts = await GetAsAt(auditDateTime,
                () => accountService.Get(valuationDate),
                asAt => accountService.Get(valuationDate, asAt));

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
            var aggregate = await GetAsAt(auditDateTime,
                () => ticketService.Get(valuationDate),
                asAt => ticketService.Get(valuationDate, asAt));
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

            return Results.Ok(await GetAsAt(auditDateTime,
                () => userService.Get(valuationDate),
                asAt => userService.Get(valuationDate, asAt)));
        };

        users.MapGet("", getUsers).ExcludeFromDescription();
        users.MapGet("/", getUsers);

        users.MapGet("/{userID:guid}", async (Guid userID, DateTime eventDateTime, DateTime? auditDateTime, UserService userService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var aggregate = await GetAsAt(auditDateTime,
                () => userService.Get(valuationDate),
                asAt => userService.Get(valuationDate, asAt));
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

            return Results.Ok(await GetAsAt(auditDateTime,
                () => userMenuPreferencesService.Get(resolvedUserID, valuationDate),
                asAt => userMenuPreferencesService.Get(resolvedUserID, valuationDate, asAt)));
        });
    }

    private static void MapUserValuationPreferencesEndpoints(this RouteGroupBuilder api)
    {
        var userValuationPreferences = api.MapGroup("/UserValuationPreferences").WithTags("User Valuation Preferences");

        userValuationPreferences.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, Guid? userID, UserValuationPreferencesService userValuationPreferencesService) =>
        {
            var resolvedUserID = new UserID(userID ?? Constants.Initialisation.UserID.Value);
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => userValuationPreferencesService.Get(resolvedUserID, valuationDate),
                asAt => userValuationPreferencesService.Get(resolvedUserID, valuationDate, asAt)));
        });
    }

    private static void MapUserBookmarksEndpoints(this RouteGroupBuilder api)
    {
        var userBookmarks = api.MapGroup("/UserBookmarks").WithTags("User Bookmarks");

        userBookmarks.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, Guid? userID, UserBookmarksService userBookmarksService) =>
        {
            var resolvedUserID = new UserID(userID ?? Constants.Initialisation.UserID.Value);
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => userBookmarksService.Get(resolvedUserID, valuationDate),
                asAt => userBookmarksService.Get(resolvedUserID, valuationDate, asAt)));
        });
    }

    private static void MapInputControlSettingsEndpoints(this RouteGroupBuilder api)
    {
        var inputControlSettings = api.MapGroup("/InputControlSettings").WithTags("Input Control Settings");

        inputControlSettings.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, InputControlSettingsService inputControlSettingsService) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

            return Results.Ok(await GetAsAt(auditDateTime,
                () => inputControlSettingsService.Get(valuationDate),
                asAt => inputControlSettingsService.Get(valuationDate, asAt)));
        });
    }

    private static void MapInputPolicyEndpoints(this RouteGroupBuilder api)
    {
        var inputPolicies = api.MapGroup("/InputPolicies").WithTags("Input Policies");

        inputPolicies.MapGet("/", async Task<IResult> (
            DateTime eventDateTime,
            DateTime? auditDateTime,
            string? controlKind,
            string? controlKinds,
            Guid? accountID,
            Guid? userID,
            string? currency,
            bool? allowNegative,
            InputPolicyService inputPolicyService) =>
        {
            if (!TryParseInputControlKinds(controlKind, controlKinds, out var resolvedControlKinds, out var parseError))
                return Results.BadRequest(new { Message = parseError });

            var request = new InputPolicyResolveRequest(
                EventDateTimeBuilder.Create(eventDateTime),
                auditDateTime.HasValue ? AuditDateTimeBuilder.Create(auditDateTime.Value) : null,
                resolvedControlKinds,
                accountID.HasValue ? AccountIDBuilder.Create(accountID.Value) : null,
                userID.HasValue ? new UserID(userID.Value) : null,
                string.IsNullOrWhiteSpace(currency) ? null : Alpha3Builder.Create(currency.Trim().ToUpperInvariant()),
                allowNegative);

            return Results.Ok(await inputPolicyService.Resolve(request));
        });
    }

    private static AuditDateTime GetAsAt(DateTime? auditDateTime) =>
        auditDateTime.HasValue
            ? AuditDateTimeBuilder.Create(auditDateTime.Value)
            : AuditDateTimeBuilder.Create();

    private static Task<T> GetAsAt<T>(
        DateTime? auditDateTime,
        Func<Task<T>> current,
        Func<AuditDateTime, Task<T>> historical) =>
        auditDateTime.HasValue
            ? historical(AuditDateTimeBuilder.Create(auditDateTime.Value))
            : current();

}
