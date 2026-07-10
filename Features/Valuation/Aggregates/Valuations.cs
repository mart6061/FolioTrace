using System.Diagnostics.CodeAnalysis;
using FolioTrace.Types;
using FolioTrace.Common;


namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Portfolio valuations")]
public sealed record Valuations : IAggregate
{
    private static readonly Dictionary<string, int> HoldingKindOrder = new(StringComparer.Ordinal)
    {
        [nameof(HoldingPositionMemo)] = 0,
        [nameof(HoldingPositionCash)] = 1,
        [nameof(HoldingPositionAsset)] = 2
    };

    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required HoldingDateBasis HoldingDateBasis { get; init; }
    public required InstrumentPriceBasis InstrumentPriceBasis { get; init; }
    public required Alpha3 ValuationCurrency { get; init; }
    public AccountID? AccountID { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<AccountValuation> Accounts { get; init; }
    public required ValuationTotals Totals { get; init; }

    [SetsRequiredMembers]
    public Valuations(
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        HoldingDateBasis holdingDateBasis,
        InstrumentPriceBasis instrumentPriceBasis,
        Alpha3 valuationCurrency,
        Accounts accounts,
        HoldingPositions positions,
        InstrumentValues instrumentValues,
        FXRates fxRates,
        AccountID? accountID = null)
    {
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        HoldingDateBasis = holdingDateBasis;
        InstrumentPriceBasis = instrumentPriceBasis;
        ValuationCurrency = valuationCurrency;
        AccountID = accountID;
        LastEventID = positions.LastEventID;
        LastAuditDateTime = new LastAuditDateTime(new[]
        {
            accounts.LastAuditDateTime.Value,
            positions.LastAuditDateTime.Value,
            instrumentValues.LastAuditDateTime.Value,
            fxRates.LastAuditDateTime.Value
        }.Max());

        var accountLookup = accounts.Items
            .Where(account => accountID is null || account.AccountID == accountID)
            .ToDictionary(account => account.AccountID.Value);
        var instrumentLookup = instrumentValues.Items.ToDictionary(instrument => instrument.InstrumentID.Value);

        var unweightedItems = positions.Items
            .Where(position => position.IncludeInValuation)
            .Where(position => accountLookup.ContainsKey(position.AccountID.Value))
            .Select(position => BuildItem(position, instrumentLookup.GetValueOrDefault(position.InstrumentID.Value), fxRates, instrumentPriceBasis, valuationCurrency))
            .ToList();
        var totalBookValue = unweightedItems.Sum(item => item.BookValue ?? 0m);
        var items = unweightedItems
            .Select(item => ApplyWeight(item, totalBookValue))
            .OrderBy(item => item.AccountName)
            .ThenBy(item => HoldingKindRank(item.HoldingKind))
            .ThenBy(item => item.Name)
            .ToList();

        Accounts = accountLookup.Values
            .OrderBy(account => account.DisplayOrder.Value)
            .ThenBy(account => account.Name)
            .Select(account =>
            {
                var accountItems = items.Where(item => item.AccountID == account.AccountID).ToList();
                return new AccountValuation
                {
                    AccountID = account.AccountID,
                    AccountName = account.Name,
                    BookCurrency = account.BookCurrency,
                    ValuationCurrency = valuationCurrency,
                    Items = accountItems,
                    Totals = BuildTotals(accountItems)
                };
            })
            .Where(account => account.Items.Count > 0)
            .ToList();

        Totals = BuildTotals(Accounts.SelectMany(account => account.Items));
    }

    private static ValuationItem BuildItem(
        HoldingPosition position,
        InstrumentValue? instrument,
        FXRates fxRates,
        InstrumentPriceBasis instrumentPriceBasis,
        Alpha3 valuationCurrency)
    {
        var priceCurrency = instrument?.PriceCurrency ?? Alpha3Builder.Create("GBP");
        var localPrice = SelectInstrumentPrice(instrument?.Price, instrumentPriceBasis);
        var fxSelection = SelectFX(fxRates, priceCurrency, valuationCurrency, instrumentPriceBasis);
        var complete = localPrice.HasValue && fxSelection.FXRate.HasValue;
        var quotePrice = complete ? localPrice.GetValueOrDefault() * fxSelection.FXRate.GetValueOrDefault() : (decimal?)null;
        var bookValue = quotePrice.HasValue ? position.Quantity * quotePrice.Value : (decimal?)null;
        var incompleteReason = BuildIncompleteReason(instrument is null, localPrice.HasValue, fxSelection.FXRate.HasValue, priceCurrency, valuationCurrency);

        return new ValuationItem
        {
            AccountID = position.AccountID,
            AccountName = position.AccountName,
            HoldingID = position.HoldingID,
            HoldingName = position.Name,
            HoldingKind = position.HoldingKind,
            InstrumentID = position.InstrumentID,
            InstrumentName = instrument?.Name ?? position.InstrumentName,
            Name = BuildName(position.Name, instrument?.Name ?? position.InstrumentName),
            PriceCurrency = priceCurrency,
            ValuationCurrency = valuationCurrency,
            FXPair = fxSelection.FXPair,
            FXDisplayPair = fxSelection.FXDisplayPair,
            FXRate = fxSelection.FXRate,
            Quantity = position.Quantity,
            LocalPrice = localPrice,
            QuotePrice = quotePrice,
            BookValue = bookValue,
            WeightPercent = null,
            BookCost = position.BookCost,
            Complete = complete,
            IncompleteReason = incompleteReason
        };
    }

    private static ValuationItem ApplyWeight(ValuationItem item, decimal totalBookValue) =>
        item with
        {
            WeightPercent = item.BookValue.HasValue && totalBookValue != 0m
                ? item.BookValue.Value / totalBookValue * 100m
                : null
        };

    private static decimal? SelectInstrumentPrice(IInstrumentPrice? price, InstrumentPriceBasis basis) =>
        price switch
        {
            InstrumentPriceCash cash => cash.Price,
            InstrumentPriceFixedIncome fixedIncome => fixedIncome.CleanPrice,
            InstrumentPriceEquity equity => basis switch
            {
                InstrumentPriceBasis.Bid => equity.Bid,
                InstrumentPriceBasis.Ask => equity.Ask,
                InstrumentPriceBasis.Mid => equity.Mid,
                InstrumentPriceBasis.NAV => equity.Nav,
                _ => equity.Mid
            },
            _ => null
        };

    private static FXSelection SelectFX(FXRates fxRates, Alpha3 priceCurrency, Alpha3 valuationCurrency, InstrumentPriceBasis basis)
    {
        if (priceCurrency == valuationCurrency)
            return new(null, null, 1m);

        var rate = fxRates.Items.FirstOrDefault(item =>
            item.Active &&
            item.BaseCurrency == priceCurrency &&
            item.QuoteCurrency == valuationCurrency);

        if (rate is null)
            return new(null, null, null);

        decimal fxRate = basis switch
        {
            InstrumentPriceBasis.Bid => rate.Price.Bid,
            InstrumentPriceBasis.Ask => rate.Price.Ask,
            InstrumentPriceBasis.Mid => rate.Price.Mid,
            InstrumentPriceBasis.NAV => rate.Price.Mid,
            _ => rate.Price.Mid
        };

        return new(rate.Pair.Value, rate.DisplayPair, fxRate);
    }

    private static string? BuildIncompleteReason(bool missingInstrument, bool hasPrice, bool hasFx, Alpha3 priceCurrency, Alpha3 valuationCurrency)
    {
        var reasons = new List<string>();
        if (missingInstrument)
            reasons.Add("Missing instrument");
        if (!hasPrice)
            reasons.Add("Missing price");
        if (!hasFx)
            reasons.Add($"Missing FX {priceCurrency}/{valuationCurrency}");

        return reasons.Count == 0 ? null : string.Join("; ", reasons);
    }

    private static string BuildName(string holdingName, string instrumentName)
    {
        if (string.IsNullOrWhiteSpace(holdingName))
            return instrumentName;

        if (string.IsNullOrWhiteSpace(instrumentName) || string.Equals(holdingName, instrumentName, StringComparison.OrdinalIgnoreCase))
            return holdingName;

        return $"{holdingName} {instrumentName}";
    }

    private static int HoldingKindRank(string holdingKind) =>
        HoldingKindOrder.TryGetValue(holdingKind, out var rank) ? rank : 100;

    private static ValuationTotals BuildTotals(IEnumerable<ValuationItem> items)
    {
        var list = items.ToList();
        return new ValuationTotals(
            list.Sum(item => item.BookValue ?? 0m),
            list.Sum(item => item.BookCost),
            list.Count(item => !item.Complete));
    }

    private sealed record FXSelection(string? FXPair, string? FXDisplayPair, decimal? FXRate);
}
