using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Services;

public sealed class ValuationService(
    AccountService accountService,
    HoldingPositionService holdingPositionService,
    InstrumentValueService instrumentValueService,
    FXRateService fxRateService)
{
    public async Task<Valuations> Get(
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis,
        Alpha3 valuationCurrency,
        AccountID? accountID = null)
    {
        var accounts = await accountService.Get(valuationDateTime, asOfDateTime);
        var positions = await holdingPositionService.Get(
            valuationDateTime,
            asOfDateTime,
            new HoldingPositionFilter(null, accountID, null, false, true),
            holdingDateBasis);
        var instrumentValues = await instrumentValueService.Get(valuationDateTime, asOfDateTime);
        var fxRates = await fxRateService.Get(valuationDateTime, asOfDateTime);

        return new Valuations(
            valuationDateTime,
            asOfDateTime,
            holdingDateBasis,
            instrumentPriceBasis,
            valuationCurrency,
            accounts,
            positions,
            instrumentValues,
            fxRates,
            accountID);
    }
}
