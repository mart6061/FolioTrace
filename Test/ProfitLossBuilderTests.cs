using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class ProfitLossBuilderTests
{
    [Fact]
    public void ProfitLosses_CalculatesLotMethodsFromTransactionMovements()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var holdings = CreateHoldings();
        var instrumentValues = CreateInstrumentValues();
        var transactions = CreateTransactions(holdings);
        var asOfDate = AuditDateTimeBuilder.Create();
        var fxRates = new FXRates(ValuationDate, asOfDate, [], []);

        var profitLosses = new ProfitLosses(
            ValuationDate,
            asOfDate,
            HoldingDateBasis.EventDateTime,
            accounts,
            holdings,
            instruments,
            instrumentValues,
            fxRates,
            transactions);

        var account = Assert.Single(profitLosses.Accounts);
        var item = Assert.Single(account.Items);
        Assert.Equal(AssetHoldingID, item.HoldingID);
        Assert.Equal(5m, item.Quantity);
        Assert.Equal(30m, item.LocalPrice);
        Assert.Equal(150m, item.MarketValue);

        AssertMethod(item, ProfitLossMethod.FIFO, realized: 250m, bookValue: 100m, unrealized: 50m, total: 300m);
        AssertMethod(item, ProfitLossMethod.LIFO, realized: 200m, bookValue: 50m, unrealized: 100m, total: 300m);
        AssertMethod(item, ProfitLossMethod.RunningAverage, realized: 225m, bookValue: 75m, unrealized: 75m, total: 300m);

        AssertMethod(account, ProfitLossMethod.FIFO, realized: 250m, bookValue: 100m, unrealized: 50m, total: 300m);
        AssertMethod(profitLosses, ProfitLossMethod.RunningAverage, realized: 225m, bookValue: 75m, unrealized: 75m, total: 300m);
    }

    [Fact]
    public void ProfitLosses_UsesAdjustedBookCostForLotMethods()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var holdings = CreateHoldings();
        var instrumentValues = CreateInstrumentValues();
        var transactions = CreateTransactions(holdings).ToList();
        var firstPurchaseSetID = transactions.OfType<TransactionCreditEvent>().First().EventSetID;
        var adjustment = TransactionBookCostAdjustedEventBuilder.Create(
            new TransactionBookCostAdjustmentRequest(UserID, "Correct first purchase", firstPurchaseSetID, new TransactionBookCost(120m)),
            transactions).Value!;
        transactions.Add(adjustment);
        var asOfDate = AuditDateTimeBuilder.Create();
        var fxRates = new FXRates(ValuationDate, asOfDate, [], []);

        var profitLosses = new ProfitLosses(
            ValuationDate,
            asOfDate,
            HoldingDateBasis.EventDateTime,
            accounts,
            holdings,
            instruments,
            instrumentValues,
            fxRates,
            transactions);

        var item = Assert.Single(Assert.Single(profitLosses.Accounts).Items);

        AssertMethod(item, ProfitLossMethod.FIFO, realized: 230m, bookValue: 100m, unrealized: 50m, total: 280m);
        AssertMethod(item, ProfitLossMethod.LIFO, realized: 190m, bookValue: 60m, unrealized: 90m, total: 280m);
        AssertMethod(item, ProfitLossMethod.RunningAverage, realized: 210m, bookValue: 80m, unrealized: 70m, total: 280m);
    }

    private static IReadOnlyList<ITransactionEvent> CreateTransactions(Holdings holdings)
    {
        var firstPurchase = CreateTransaction(
            EventDateTimeBuilder.Create(new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)),
            "Buy first lot",
            credits: [CreateLeg(AssetHoldingID, 10m, 100m)],
            debits: [CreateLeg(OutflowHoldingID, 100m, 100m)],
            holdings);
        var secondPurchase = CreateTransaction(
            EventDateTimeBuilder.Create(new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc)),
            "Buy second lot",
            credits: [CreateLeg(AssetHoldingID, 10m, 200m)],
            debits: [CreateLeg(OutflowHoldingID, 200m, 200m)],
            holdings);
        var sale = CreateTransaction(
            EventDateTimeBuilder.Create(new DateTime(2026, 6, 3, 10, 0, 0, DateTimeKind.Utc)),
            "Sell partial position",
            credits: [CreateLeg(InflowHoldingID, 450m, 450m)],
            debits: [CreateLeg(AssetHoldingID, 15m, 450m)],
            holdings);

        return firstPurchase.Concat(secondPurchase).Concat(sale).Cast<ITransactionEvent>().ToList();
    }

    private static IReadOnlyList<ITransactionMovementEvent> CreateTransaction(
        EventDateTime eventDateTime,
        string reason,
        IReadOnlyList<TransactionRequest> credits,
        IReadOnlyList<TransactionRequest> debits,
        Holdings holdings)
    {
        var result = TransactionBuilder.Create(
            new TransactionSetRequest(
                UserID,
                eventDateTime,
                SettlementDateTimeBuilder.Create(eventDateTime.Value.AddDays(1)),
                reason,
                credits,
                debits),
            holdings);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
        return result.Value!;
    }

    private static TransactionRequest CreateLeg(HoldingID holdingID, decimal quantity, decimal bookCost) =>
        new(
            holdingID,
            InstrumentID,
            AccountID,
            new TransactionQuantity(quantity),
            new TransactionLocalCost(bookCost),
            Alpha3Builder.Create("GBP"),
            new TransactionBookCost(bookCost),
            BookCostSource.SameCurrency,
            false);

    private static Accounts CreateAccounts()
    {
        var created = AccountCreatedEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            SetupDate,
            SetupAuditDate,
            "Create account",
            AccountID,
            "General",
            "General Account",
            Alpha3Builder.Create("GBP"),
            true).Value!;

        return new Accounts(SetupDate, SetupAuditDate, [created]);
    }

    private static Instruments CreateInstruments()
    {
        var created = InstrumentCreatedEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            SetupDate,
            SetupAuditDate,
            "Create instrument",
            InstrumentID,
            "Example Equity",
            "Example Equity plc",
            ExchangeBuilder.Create("XLON"),
            CFIBuilder.Create("ESVUFR"),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create("GBP")).Value!;

        return new Instruments(SetupDate, SetupAuditDate, [created]);
    }

    private static Holdings CreateHoldings()
    {
        var events = new HoldingCreatedEvent[]
        {
            HoldingPositionAssetCreatedEventBuilder.CreateSeed(
                CreateEventID(),
                UserID,
                SetupDate,
                SetupAuditDate,
                "Create asset holding",
                AssetHoldingID,
                AccountID,
                InstrumentID,
                "Example position",
                true,
                true).Value!,
            HoldingNominalOutflowCreatedEventBuilder.CreateSeed(
                CreateEventID(),
                UserID,
                SetupDate,
                SetupAuditDate,
                "Create outflow holding",
                OutflowHoldingID,
                AccountID,
                InstrumentID,
                "Outflow",
                true,
                false).Value!,
            HoldingNominalInflowCreatedEventBuilder.CreateSeed(
                CreateEventID(),
                UserID,
                SetupDate,
                SetupAuditDate,
                "Create inflow holding",
                InflowHoldingID,
                AccountID,
                InstrumentID,
                "Inflow",
                true,
                false).Value!
        };

        return new Holdings(SetupDate, SetupAuditDate, events.Cast<IHoldingEvent>().ToList());
    }

    private static InstrumentValues CreateInstrumentValues()
    {
        var instrument = InstrumentCreatedEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            SetupDate,
            SetupAuditDate,
            "Create instrument",
            InstrumentID,
            "Example Equity",
            "Example Equity plc",
            ExchangeBuilder.Create("XLON"),
            CFIBuilder.Create("ESVUFR"),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create("GBP")).Value!;
        var price = InstrumentPriceSetEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            SetupAuditDate,
            "Set price",
            InstrumentID,
            new InstrumentPriceEquity(
                new InstrumentPrice(30m),
                new InstrumentPrice(30m),
                new InstrumentPrice(30m),
                new InstrumentPrice(30m))).Value!;
        var income = InstrumentIncomeSetEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            SetupAuditDate,
            "Set income",
            InstrumentID,
            new InstrumentIncomeEquity(
                new InstrumentPrice(0m),
                "Regular",
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 1)),
                InstrumentDateBuilder.Create(new DateOnly(2025, 12, 1)),
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 2)),
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 31)))).Value!;

        return new InstrumentValues(ValuationDate, SetupAuditDate, [instrument], [price], [income]);
    }

    private static void AssertMethod(ProfitLossItem item, ProfitLossMethod method, decimal realized, decimal bookValue, decimal unrealized, decimal total) =>
        AssertMethodValue(item.Methods.Single(value => value.Method == method), realized, bookValue, unrealized, total);

    private static void AssertMethod(AccountProfitLoss account, ProfitLossMethod method, decimal realized, decimal bookValue, decimal unrealized, decimal total) =>
        AssertMethodValue(account.Totals.Single(value => value.Method == method), realized, bookValue, unrealized, total);

    private static void AssertMethod(ProfitLosses profitLosses, ProfitLossMethod method, decimal realized, decimal bookValue, decimal unrealized, decimal total) =>
        AssertMethodValue(profitLosses.Totals.Single(value => value.Method == method), realized, bookValue, unrealized, total);

    private static void AssertMethodValue(ProfitLossMethodValue value, decimal realized, decimal bookValue, decimal unrealized, decimal total)
    {
        Assert.True(value.Complete, value.IncompleteReason);
        Assert.Equal(realized, value.RealizedPnL);
        Assert.Equal(bookValue, value.BookValue);
        Assert.Equal(unrealized, value.UnrealizedPnL);
        Assert.Equal(total, value.TotalPnL);
    }

    private static EventID CreateEventID() => new(Guid.CreateGuid7());

    private static readonly UserID UserID = new(Guid.Parse("48c885d1-8e98-4fe7-a797-77420fbfb449"));
    private static readonly EventDateTime SetupDate = EventDateTimeBuilder.Create(new DateTime(2026, 5, 31, 9, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime SetupAuditDate = AuditDateTimeBuilder.Create(new DateTime(2026, 5, 31, 9, 1, 0, DateTimeKind.Utc));
    private static readonly EventDateTime ValuationDate = EventDateTimeBuilder.Create(new DateTime(2026, 6, 4, 9, 0, 0, DateTimeKind.Utc));
    private static readonly AccountID AccountID = AccountIDBuilder.Create();
    private static readonly InstrumentID InstrumentID = InstrumentIDBuilder.Create();
    private static readonly HoldingID AssetHoldingID = HoldingIDBuilder.Create();
    private static readonly HoldingID OutflowHoldingID = HoldingIDBuilder.Create();
    private static readonly HoldingID InflowHoldingID = HoldingIDBuilder.Create();
}
