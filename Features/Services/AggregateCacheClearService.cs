namespace Services;

public sealed class AggregateCacheClearService(
    CountryService countryService,
    CurrencyService currencyService,
    FXService fxService,
    FXRateService fxRateService,
    InstrumentService instrumentService,
    InstrumentValueService instrumentValueService)
{
    public AggregateCacheClearResult ClearAll()
    {
        var countries = countryService.InvalidateAll();
        var currencies = currencyService.InvalidateAll();
        var fxs = fxService.InvalidateAll();
        var fxRates = fxRateService.InvalidateAll();
        var instruments = instrumentService.InvalidateAll();
        var instrumentValues = instrumentValueService.InvalidateAll();

        return new AggregateCacheClearResult(countries, currencies, fxs, fxRates, instruments, instrumentValues);
    }
}

public sealed record AggregateCacheClearResult(
    int Countries,
    int Currencies,
    int FXs,
    int FXRates,
    int Instruments,
    int InstrumentValues);
