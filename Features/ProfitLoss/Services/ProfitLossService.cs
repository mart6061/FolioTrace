using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class ProfitLossService(
    IEventRepository eventRepository,
    AccountService accountService,
    HoldingService holdingService,
    InstrumentService instrumentService,
    InstrumentValueService instrumentValueService,
    FXRateService fxRateService)
{
    public async Task<ProfitLosses> Get(
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis,
        AccountID? accountID = null)
    {
        var accounts = await accountService.Get(valuationDateTime, asOfDateTime);
        var holdings = await holdingService.Get(valuationDateTime, asOfDateTime);
        var instruments = await instrumentService.Get(valuationDateTime, asOfDateTime);
        var instrumentValues = await instrumentValueService.Get(valuationDateTime, asOfDateTime);
        var fxRates = await fxRateService.Get(valuationDateTime, asOfDateTime);
        var transactionEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId);

        return new ProfitLosses(
            valuationDateTime,
            asOfDateTime,
            holdingDateBasis,
            accounts,
            holdings,
            instruments,
            instrumentValues,
            fxRates,
            transactionEvents,
            instrumentPriceBasis,
            accountID);
    }
}
