using System.Diagnostics.CodeAnalysis;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ProfitLosses : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required HoldingDateBasis HoldingDateBasis { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required IReadOnlyList<AccountProfitLoss> Accounts { get; init; }

    public required IReadOnlyList<ProfitLossMethodValue> Totals { get; init; }

    [SetsRequiredMembers]
    public ProfitLosses(
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        HoldingDateBasis holdingDateBasis,
        Accounts accounts,
        Holdings holdings,
        Instruments instruments,
        InstrumentValues instrumentValues,
        FXRates fxRates,
        IReadOnlyList<ITransactionEvent> transactionEvents,
        InstrumentPriceBasis instrumentPriceBasis = InstrumentPriceBasis.Mid,
        AccountID? accountID = null)
    {
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        HoldingDateBasis = holdingDateBasis;

        var movements = TransactionEventSelector.GetActiveMovements(transactionEvents, asOfDateTime)
            .Where(movement => IsIncluded(movement, valuationDateTime, holdingDateBasis))
            .Where(movement => accountID is null || movement.AccountID == accountID)
            .ToList();
        var latestSource = LatestSource(accounts, holdings, instruments, instrumentValues, fxRates, movements);
        LastEventID = latestSource.EventID;
        LastAuditDateTime = new LastAuditDateTime(latestSource.AuditDateTime);

        var accountLookup = accounts.Items
            .Where(account => accountID is null || account.AccountID == accountID)
            .ToDictionary(account => account.AccountID.Value);
        var holdingLookup = holdings.Items.ToDictionary(holding => holding.HoldingID.Value);
        var instrumentLookup = instrumentValues.Items.ToDictionary(instrument => instrument.InstrumentID.Value);
        var instrumentDefinitionLookup = instruments.Items.ToDictionary(instrument => instrument.InstrumentID.Value);

        var items = movements
            .Where(movement => holdingLookup.TryGetValue(movement.HoldingID.Value, out var holding) && holding.IncludeInValuation)
            .Where(movement => accountLookup.ContainsKey(movement.AccountID.Value))
            .GroupBy(movement => movement.HoldingID.Value)
            .Select(group => BuildItem(
                group.ToList(),
                holdingLookup[group.Key],
                accountLookup[group.First().AccountID.Value],
                instrumentLookup.GetValueOrDefault(group.First().InstrumentID.Value),
                instrumentDefinitionLookup.GetValueOrDefault(group.First().InstrumentID.Value),
                fxRates,
                instrumentPriceBasis,
                holdingDateBasis))
            .OrderBy(item => item.AccountName)
            .ThenBy(item => item.HoldingKind)
            .ThenBy(item => item.HoldingName)
            .ToList();

        Accounts = accountLookup.Values
            .OrderBy(account => account.DisplayOrder.Value)
            .ThenBy(account => account.Name)
            .Select(account =>
            {
                var accountItems = items.Where(item => item.AccountID == account.AccountID).ToList();
                return new AccountProfitLoss
                {
                    AccountID = account.AccountID,
                    AccountName = account.Name,
                    BookCurrency = account.BookCurrency,
                    Items = accountItems,
                    Totals = BuildTotals(accountItems)
                };
            })
            .Where(account => account.Items.Count > 0)
            .ToList();

        Totals = BuildTotals(Accounts.SelectMany(account => account.Items));
    }

    private static ProfitLossItem BuildItem(
        IReadOnlyList<ITransactionMovementEvent> movements,
        HoldingBase holding,
        Account account,
        InstrumentValue? instrumentValue,
        Instrument? instrumentDefinition,
        FXRates fxRates,
        InstrumentPriceBasis instrumentPriceBasis,
        HoldingDateBasis holdingDateBasis)
    {
        var orderedMovements = movements
            .OrderBy(movement => MovementDate(movement, movement.SettlementDateTime, holdingDateBasis))
            .ThenBy(movement => movement.AuditDateTime.Value)
            .ThenBy(movement => movement.EventID.Value)
            .ToList();
        var quantity = orderedMovements.Sum(SignedQuantity);
        var bookCost = orderedMovements.Sum(SignedBookCost);
        var priceCurrency = instrumentValue?.PriceCurrency ?? instrumentDefinition?.PriceCurrency ?? Alpha3Builder.Create("GBP");
        var localPrice = SelectInstrumentPrice(instrumentValue?.Price, instrumentPriceBasis);
        var bookPrice = SelectBookPrice(localPrice, priceCurrency, account.BookCurrency, fxRates);
        var marketValue = bookPrice.HasValue ? quantity * bookPrice.Value : (decimal?)null;
        var methodValues = Enum.GetValues<ProfitLossMethod>()
            .Select(method => CalculateMethod(orderedMovements, method, marketValue, quantity, localPrice.HasValue, bookPrice.HasValue, priceCurrency, account.BookCurrency))
            .ToList();

        return new ProfitLossItem
        {
            AccountID = account.AccountID,
            AccountName = account.Name,
            BookCurrency = account.BookCurrency,
            HoldingID = holding.HoldingID,
            HoldingName = holding.Name,
            HoldingKind = holding.GetHoldingKindName(),
            InstrumentID = holding.InstrumentID,
            InstrumentName = instrumentValue?.Name ?? instrumentDefinition?.Name ?? holding.Name,
            PriceCurrency = priceCurrency,
            Quantity = quantity,
            LocalPrice = localPrice,
            BookPrice = bookPrice,
            MarketValue = marketValue,
            BookCost = bookCost,
            Methods = methodValues
        };
    }

    private static ProfitLossMethodValue CalculateMethod(
        IReadOnlyList<ITransactionMovementEvent> movements,
        ProfitLossMethod method,
        decimal? marketValue,
        decimal finalQuantity,
        bool hasPrice,
        bool hasBookPrice,
        Alpha3 priceCurrency,
        Alpha3 bookCurrency)
    {
        var complete = true;
        var reasons = new List<string>();
        var realized = 0m;

        if (method == ProfitLossMethod.RunningAverage)
        {
            var quantity = 0m;
            var cost = 0m;
            foreach (var movement in movements)
            {
                if (TransactionEventSelector.IsCredit(movement))
                {
                    quantity += movement.Quantity.Value;
                    cost += movement.BookCost.Value;
                    continue;
                }

                if (!TransactionEventSelector.IsDebit(movement))
                    continue;

                var disposeQuantity = movement.Quantity.Value;
                var availableQuantity = quantity;
                if (availableQuantity <= 0m || disposeQuantity > availableQuantity)
                {
                    complete = false;
                    reasons.Add("Short position or over-disposal handling is incomplete in v1.");
                }

                var consumedQuantity = Math.Min(disposeQuantity, Math.Max(availableQuantity, 0m));
                var consumedCost = availableQuantity == 0m ? 0m : cost / availableQuantity * consumedQuantity;
                realized += movement.BookCost.Value - consumedCost;
                quantity -= disposeQuantity;
                cost -= consumedCost;
            }

            return BuildMethodValue(method, realized, marketValue, cost, finalQuantity, complete, reasons, hasPrice, hasBookPrice, priceCurrency, bookCurrency);
        }

        var lots = new List<ProfitLossLot>();
        foreach (var movement in movements)
        {
            if (TransactionEventSelector.IsCredit(movement))
            {
                lots.Add(new ProfitLossLot(movement.Quantity.Value, movement.BookCost.Value));
                continue;
            }

            if (!TransactionEventSelector.IsDebit(movement))
                continue;

            var remainingDisposeQuantity = movement.Quantity.Value;
            var proceedsRemaining = movement.BookCost.Value;
            while (remainingDisposeQuantity > 0m)
            {
                if (lots.Count == 0)
                {
                    complete = false;
                    reasons.Add("Short position or over-disposal handling is incomplete in v1.");
                    realized += proceedsRemaining;
                    break;
                }

                var lotIndex = method == ProfitLossMethod.FIFO ? 0 : lots.Count - 1;
                var lot = lots[lotIndex];
                var consumedQuantity = Math.Min(remainingDisposeQuantity, lot.Quantity);
                var quantityRatio = consumedQuantity / remainingDisposeQuantity;
                var proceeds = proceedsRemaining * quantityRatio;
                var consumedCost = lot.Cost * consumedQuantity / lot.Quantity;
                realized += proceeds - consumedCost;

                remainingDisposeQuantity -= consumedQuantity;
                proceedsRemaining -= proceeds;
                var remainingLotQuantity = lot.Quantity - consumedQuantity;
                if (remainingLotQuantity <= 0m)
                    lots.RemoveAt(lotIndex);
                else
                    lots[lotIndex] = new ProfitLossLot(remainingLotQuantity, lot.Cost - consumedCost);
            }
        }

        var remainingCost = lots.Sum(lot => lot.Cost);
        return BuildMethodValue(method, realized, marketValue, remainingCost, finalQuantity, complete, reasons, hasPrice, hasBookPrice, priceCurrency, bookCurrency);
    }

    private static ProfitLossMethodValue BuildMethodValue(
        ProfitLossMethod method,
        decimal realized,
        decimal? marketValue,
        decimal remainingCost,
        decimal finalQuantity,
        bool complete,
        List<string> reasons,
        bool hasPrice,
        bool hasBookPrice,
        Alpha3 priceCurrency,
        Alpha3 bookCurrency)
    {
        if (finalQuantity != 0m)
        {
            if (!hasPrice)
            {
                complete = false;
                reasons.Add("Missing price.");
            }

            if (!hasBookPrice)
            {
                complete = false;
                reasons.Add($"Missing FX {priceCurrency}/{bookCurrency}.");
            }
        }

        var unrealized = marketValue.HasValue ? marketValue.Value - remainingCost : (decimal?)null;

        return new ProfitLossMethodValue
        {
            Method = method,
            RealizedPnL = decimal.Round(realized, 8),
            BookValue = decimal.Round(remainingCost, 8),
            UnrealizedPnL = unrealized.HasValue ? decimal.Round(unrealized.Value, 8) : null,
            TotalPnL = unrealized.HasValue ? decimal.Round(realized + unrealized.Value, 8) : null,
            Complete = complete,
            IncompleteReason = reasons.Count == 0 ? null : string.Join("; ", reasons.Distinct(StringComparer.Ordinal))
        };
    }

    private static IReadOnlyList<ProfitLossMethodValue> BuildTotals(IEnumerable<ProfitLossItem> items)
    {
        var itemList = items.ToList();
        return Enum.GetValues<ProfitLossMethod>()
            .Select(method =>
            {
                var methodValues = itemList
                    .SelectMany(item => item.Methods)
                    .Where(value => value.Method == method)
                    .ToList();
                var hasIncomplete = methodValues.Any(value => !value.Complete);
                var hasMissingUnrealized = methodValues.Any(value => !value.UnrealizedPnL.HasValue);
                var unrealized = hasMissingUnrealized ? (decimal?)null : methodValues.Sum(value => value.UnrealizedPnL!.Value);

                return new ProfitLossMethodValue
                {
                    Method = method,
                    RealizedPnL = methodValues.Sum(value => value.RealizedPnL),
                    BookValue = methodValues.Sum(value => value.BookValue),
                    UnrealizedPnL = unrealized,
                    TotalPnL = unrealized.HasValue ? methodValues.Sum(value => value.RealizedPnL) + unrealized.Value : null,
                    Complete = !hasIncomplete,
                    IncompleteReason = hasIncomplete
                        ? string.Join("; ", methodValues.Select(value => value.IncompleteReason).Where(reason => !string.IsNullOrWhiteSpace(reason)).Distinct(StringComparer.Ordinal))
                        : null
                };
            })
            .ToList();
    }

    private static bool IsIncluded(ITransactionMovementEvent movement, EventDateTime valuationDateTime, HoldingDateBasis holdingDateBasis) =>
        MovementDate(movement, movement.SettlementDateTime, holdingDateBasis) <= valuationDateTime.Value;

    private static DateTime MovementDate(ITransactionMovementEvent movement, SettlementDateTime settlementDateTime, HoldingDateBasis holdingDateBasis) =>
        holdingDateBasis == HoldingDateBasis.SettlementDateTime
            ? settlementDateTime.Value
            : movement.EventDateTime.Value;

    private static decimal SignedQuantity(ITransactionMovementEvent movement) =>
        TransactionEventSelector.IsCredit(movement)
            ? movement.Quantity.Value
            : TransactionEventSelector.IsDebit(movement)
                ? -movement.Quantity.Value
                : 0m;

    private static decimal SignedBookCost(ITransactionMovementEvent movement) =>
        TransactionEventSelector.IsCredit(movement)
            ? movement.BookCost.Value
            : TransactionEventSelector.IsDebit(movement)
                ? -movement.BookCost.Value
                : 0m;

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

    private static decimal? SelectBookPrice(decimal? localPrice, Alpha3 priceCurrency, Alpha3 bookCurrency, FXRates fxRates)
    {
        if (!localPrice.HasValue)
            return null;

        if (priceCurrency == bookCurrency)
            return localPrice.Value;

        var rate = fxRates.Items.FirstOrDefault(item =>
            item.Active &&
            item.BaseCurrency == priceCurrency &&
            item.QuoteCurrency == bookCurrency);

        return rate is null ? null : localPrice.Value * rate.Price.Mid;
    }

    private static (EventID EventID, DateTime AuditDateTime) LatestSource(
        Accounts accounts,
        Holdings holdings,
        Instruments instruments,
        InstrumentValues instrumentValues,
        FXRates fxRates,
        IReadOnlyList<ITransactionMovementEvent> movements)
    {
        var sources = new List<(EventID EventID, DateTime AuditDateTime)>
        {
            (accounts.LastEventID, accounts.LastAuditDateTime.Value),
            (holdings.LastEventID, holdings.LastAuditDateTime.Value),
            (instruments.LastEventID, instruments.LastAuditDateTime.Value),
            (instrumentValues.LastEventID, instrumentValues.LastAuditDateTime.Value),
            (fxRates.LastEventID, fxRates.LastAuditDateTime.Value)
        };
        sources.AddRange(movements.Select(movement => (movement.EventID, movement.AuditDateTime.Value)));

        return sources
            .OrderBy(source => source.AuditDateTime)
            .ThenBy(source => source.EventID.Value)
            .Last();
    }

    private sealed record ProfitLossLot(decimal Quantity, decimal Cost);
}
