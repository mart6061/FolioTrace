namespace Services;

public sealed class AggregateCacheClearService(
    AccountService accountService,
    BrokerService brokerService,
    CountryService countryService,
    CurrencyService currencyService,
    FXService fxService,
    FXRateService fxRateService,
    HoldingService holdingService,
    HoldingPositionService holdingPositionService,
    InstrumentService instrumentService,
    InstrumentValueService instrumentValueService,
    TicketService ticketService,
    UserService userService,
    UserMenuPreferencesService userMenuPreferencesService,
    UserValuationPreferencesService userValuationPreferencesService,
    UserBookmarksService userBookmarksService,
    ValuationSettingService valuationSettingService,
    AssetAllocationMappingService assetAllocationMappingService,
    ReportConfigService reportConfigService)
{
    public AggregateCacheClearResult ClearAll()
    {
        var accounts = accountService.InvalidateAll();
        var brokers = brokerService.InvalidateAll();
        var countries = countryService.InvalidateAll();
        var currencies = currencyService.InvalidateAll();
        var fxs = fxService.InvalidateAll();
        var fxRates = fxRateService.InvalidateAll();
        var holdings = holdingService.InvalidateAll();
        var holdingPositions = holdingPositionService.InvalidateAll();
        var instruments = instrumentService.InvalidateAll();
        var instrumentValues = instrumentValueService.InvalidateAll();
        var tickets = ticketService.InvalidateAll();
        var users = userService.InvalidateAll();
        var userMenuPreferences = userMenuPreferencesService.InvalidateAll();
        var userValuationPreferences = userValuationPreferencesService.InvalidateAll();
        var userBookmarks = userBookmarksService.InvalidateAll();
        var valuationSettings = valuationSettingService.InvalidateAll();
        var assetAllocationMappings = assetAllocationMappingService.InvalidateAll();
        var reportConfigs = reportConfigService.InvalidateAll();

        return new AggregateCacheClearResult(accounts, brokers, countries, currencies, fxs, fxRates, holdings, holdingPositions, instruments, instrumentValues, tickets, users, userMenuPreferences, userValuationPreferences, userBookmarks, valuationSettings, assetAllocationMappings, reportConfigs);
    }
}

public sealed record AggregateCacheClearResult(
    int Accounts,
    int Brokers,
    int Countries,
    int Currencies,
    int FXs,
    int FXRates,
    int Holdings,
    int HoldingPositions,
    int Instruments,
    int InstrumentValues,
    int Tickets,
    int Users,
    int UserMenuPreferences,
    int UserValuationPreferences,
    int UserBookmarks,
    int ValuationSettings,
    int AssetAllocationMappings,
    int ReportConfigs);
