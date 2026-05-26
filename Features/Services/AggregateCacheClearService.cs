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
    InstrumentValueService instrumentValueService)
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

        return new AggregateCacheClearResult(accounts, countries, currencies, fxs, fxRates, holdings, holdingPositions, instruments, instrumentValues);
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
    int InstrumentValues);
