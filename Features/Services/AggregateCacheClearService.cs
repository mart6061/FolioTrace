namespace Services;

public sealed class AggregateCacheClearService(
    AccountService accountService,
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
    UserBookmarksService userBookmarksService)
{
    public AggregateCacheClearResult ClearAll()
    {
        var accounts = accountService.InvalidateAll();
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

        return new AggregateCacheClearResult(accounts, countries, currencies, fxs, fxRates, holdings, holdingPositions, instruments, instrumentValues, tickets, users, userMenuPreferences, userValuationPreferences, userBookmarks);
    }
}

public sealed record AggregateCacheClearResult(
    int Accounts,
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
    int UserBookmarks);
