using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class ValuationBuilderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Valuations_ScalesOptionContractsAndZerosThemAfterExpiryDay(bool useLegacyAssetHolding)
    {
        var accountID = AccountIDBuilder.Create();
        var underlyingID = InstrumentIDBuilder.Create();
        var optionID = InstrumentIDBuilder.Create();
        var holdingID = HoldingIDBuilder.Create();
        var outflowHoldingID = HoldingIDBuilder.Create();
        var accounts = CreateAccounts(accountID);
        var optionTerms = new InstrumentTermsOption(
            OptionType.Call,
            underlyingID,
            new Money(100m, new Alpha3("GBP")),
            new InstrumentDate(new DateOnly(2026, 6, 18)),
            OptionExerciseStyle.European,
            OptionSettlementType.Cash,
            new ContractMultiplier(100m));
        var instrumentEvents = new IInstrumentEvent[]
        {
            CreateInstrument(underlyingID, "Underlying"),
            CreateInstrument(optionID, "Underlying 100 Call", "OCXXXX"),
            InstrumentTermsSetEventBuilder.CreateSeed(CreateEventID(), UserID, ValuationDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(1)), "Set option terms", optionID, optionTerms).Value!
        };
        var instruments = new Instruments(ValuationDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(2)), instrumentEvents.ToList());
        var holdings = new Holdings(ValuationDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(2)),
            [useLegacyAssetHolding ? CreateAssetHolding(holdingID, accountID, optionID, "Option position") : CreateOptionHolding(holdingID, accountID, optionID, "Option position"), CreateOutflowHolding(outflowHoldingID, accountID, optionID)]);
        var transactions = TransactionBuilder.Create(
            new TransactionSetRequest(UserID, ValuationDate, SettlementDateTimeBuilder.Create(ValuationDate.Value.AddDays(1)), "Book option",
                [CreateTransactionLeg(holdingID, optionID, accountID, 3m, 750m)],
                [CreateTransactionLeg(outflowHoldingID, optionID, accountID, 3m, 750m)]), holdings).Value!.Cast<ITransactionEvent>().ToList();
        var asOf = AuditDateTimeBuilder.Create();
        var positions = new HoldingPositions(ValuationDate, asOf, holdings, accounts, instruments, transactions);
        var price = InstrumentPriceSetEventBuilder.CreateSeed(CreateEventID(), UserID, ValuationDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(3)), "Set option price", optionID,
            new InstrumentPriceOption(new InstrumentQuote(new InstrumentPrice(2.4m), new InstrumentPrice(2.5m), new InstrumentPrice(2.6m)), new InstrumentPrice(2.45m))).Value!;
        var values = new InstrumentValues(ValuationDate, asOf, instrumentEvents.ToList(), [price], []);
        var fxRates = new FXRates(ValuationDate, asOf, [], []);

        var active = new Valuations(ValuationDate, asOf, HoldingDateBasis.EventDateTime, InstrumentPriceBasis.Mid,
            ValuationPriceConvention.Dirty, new Alpha3("GBP"), accounts, positions, values, fxRates);
        var expiredDate = EventDateTimeBuilder.Create(new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc));
        var expired = new Valuations(expiredDate, asOf, HoldingDateBasis.EventDateTime, InstrumentPriceBasis.Mid,
            ValuationPriceConvention.Dirty, new Alpha3("GBP"), accounts, positions, values, fxRates);

        var activeItem = Assert.Single(active.Accounts.Single().Items);
        Assert.Equal(100m, activeItem.ContractMultiplier);
        Assert.Equal(750m, activeItem.BookValue);
        Assert.False(activeItem.Option!.Expired);
        var expiredItem = Assert.Single(expired.Accounts.Single().Items);
        Assert.Equal(0m, expiredItem.BookValue);
        Assert.Equal(0m, expiredItem.LocalPrice);
        Assert.True(expiredItem.Option!.Expired);
        Assert.True(expiredItem.Complete);
    }

    [Fact]
    public void Valuations_CalculatesWeightAsShareOfTotalBookValue()
    {
        var accountID = AccountIDBuilder.Create();
        var firstInstrumentID = InstrumentIDBuilder.Create();
        var secondInstrumentID = InstrumentIDBuilder.Create();
        var firstHoldingID = HoldingIDBuilder.Create();
        var secondHoldingID = HoldingIDBuilder.Create();
        var outflowHoldingID = HoldingIDBuilder.Create();
        var accounts = CreateAccounts(accountID);
        var instrumentEvents = new[]
        {
            CreateInstrument(firstInstrumentID, "First equity"),
            CreateInstrument(secondInstrumentID, "Second equity")
        };
        var instruments = new Instruments(ValuationDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(1)), instrumentEvents.Cast<IInstrumentEvent>().ToList());
        var holdings = new Holdings(
            ValuationDate,
            AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(2)),
            new HoldingCreatedEvent[]
            {
                CreateAssetHolding(firstHoldingID, accountID, firstInstrumentID, "First position"),
                CreateAssetHolding(secondHoldingID, accountID, secondInstrumentID, "Second position"),
                CreateOutflowHolding(outflowHoldingID, accountID, firstInstrumentID)
            }.Cast<IHoldingEvent>().ToList());
        var transactions = TransactionBuilder.Create(
            new TransactionSetRequest(
                UserID,
                ValuationDate,
                SettlementDateTimeBuilder.Create(ValuationDate.Value.AddDays(1)),
                "Book holdings",
                [
                    CreateTransactionLeg(firstHoldingID, firstInstrumentID, accountID, 10m, 250m),
                    CreateTransactionLeg(secondHoldingID, secondInstrumentID, accountID, 30m, 750m)
                ],
                [CreateTransactionLeg(outflowHoldingID, firstInstrumentID, accountID, 40m, 1000m)]),
            holdings).Value!.Cast<ITransactionEvent>().ToList();
        var asOfDate = AuditDateTimeBuilder.Create();
        var positions = new HoldingPositions(ValuationDate, asOfDate, holdings, accounts, instruments, transactions);
        Assert.Equal(2, positions.Items.Count);
        Assert.Contains(positions.Items, position => position.HoldingID == firstHoldingID && position.Quantity == 10m);
        Assert.Contains(positions.Items, position => position.HoldingID == secondHoldingID && position.Quantity == 30m);
        var instrumentValues = new InstrumentValues(
            ValuationDate,
            asOfDate,
            instrumentEvents.Cast<IInstrumentEvent>().ToList(),
            [
                CreatePrice(firstInstrumentID, 10m),
                CreatePrice(secondInstrumentID, 10m)
            ],
            [
                CreateIncome(firstInstrumentID),
                CreateIncome(secondInstrumentID)
            ]);
        Assert.Equal(2, instrumentValues.Items.Count);
        Assert.All(instrumentValues.Items, item => Assert.NotNull(item.Price));
        var fxRates = new FXRates(ValuationDate, asOfDate, [], []);

        var valuations = new Valuations(
            ValuationDate,
            asOfDate,
            HoldingDateBasis.EventDateTime,
            InstrumentPriceBasis.Mid,
            ValuationPriceConvention.Dirty,
            Alpha3Builder.Create("GBP"),
            accounts,
            positions,
            instrumentValues,
            fxRates);

        Assert.Equal(400m, valuations.Totals.BookValue);
        Assert.Equal(2, valuations.Accounts.Single().Items.Count);

        var first = valuations.Accounts.Single().Items.Single(item => item.HoldingID == firstHoldingID);
        var second = valuations.Accounts.Single().Items.Single(item => item.HoldingID == secondHoldingID);
        Assert.Equal(100m, first.BookValue);
        Assert.Equal(25m, first.WeightPercent);
        Assert.Equal(300m, second.BookValue);
        Assert.Equal(75m, second.WeightPercent);
    }

    [Theory]
    [InlineData(InstrumentPriceBasis.Bid, 0.75)]
    [InlineData(InstrumentPriceBasis.Mid, 0.76)]
    [InlineData(InstrumentPriceBasis.Ask, 0.77)]
    public void Valuations_DirtyExceedsCleanByTheAccruedInterest(InstrumentPriceBasis basis, double fxRate)
    {
        // Clean and dirty are two routes to the same number. The gap between them is the accrued interest,
        // per unit like a price, converted at the same rate the price used: any other rate and the two drift
        // apart by the spread while each still looks internally consistent.
        var bond = CreateBondValuation();
        var expectedAccrued = AccruedInterestPerUnit * PositionQuantity * (decimal)fxRate;

        var clean = bond.Value(basis, ValuationPriceConvention.Clean);
        var dirty = bond.Value(basis, ValuationPriceConvention.Dirty);

        var cleanItem = clean.Accounts.Single().Items.Single();
        var dirtyItem = dirty.Accounts.Single().Items.Single();
        Assert.Equal(CleanQuote(basis), cleanItem.LocalPrice);
        Assert.Equal(CleanQuote(basis) + AccruedInterestPerUnit, dirtyItem.LocalPrice);
        Assert.Equal((decimal)fxRate, cleanItem.FXRate);
        Assert.Equal((decimal)fxRate, dirtyItem.FXRate);
        Assert.Equal(expectedAccrued, dirtyItem.BookValue!.Value - cleanItem.BookValue!.Value);
        // The final total is the same under either convention, so the gap shows up against the clean subtotal.
        Assert.Equal(expectedAccrued, clean.Totals.BookValue - clean.Totals.CleanValue);
        Assert.Equal(clean.Totals.BookValue, dirty.Totals.BookValue);
    }

    [Theory]
    [InlineData(InstrumentPriceBasis.Bid)]
    [InlineData(InstrumentPriceBasis.Mid)]
    [InlineData(InstrumentPriceBasis.Ask)]
    public void Valuations_ReconcilesCleanSubtotalAccruedAndFinalTotal(InstrumentPriceBasis basis)
    {
        // cleanSubtotal + totalAccruedInterest == finalTotal == dirtyTotal. If these ever disagree the toggle
        // has become a correctness bug rather than a display option, and nothing else here would catch it.
        var bond = CreateBondValuation();

        var clean = bond.Value(basis, ValuationPriceConvention.Clean).Totals;
        var dirty = bond.Value(basis, ValuationPriceConvention.Dirty).Totals;

        Assert.Equal(clean.BookValue, clean.CleanValue + clean.AccruedValue);
        Assert.Equal(dirty.BookValue, dirty.CleanValue + dirty.AccruedValue);
        Assert.Equal(dirty.BookValue, clean.BookValue);
        Assert.Equal(dirty.AccruedValue, clean.AccruedValue);
        Assert.Equal(dirty.CleanValue, clean.CleanValue);
    }

    [Fact]
    public void Valuations_ScalesAccruedInterestByQuantityAndTheFXRateThePriceUsed()
    {
        // Accrued interest is stored per unit, like a price, and must convert at the rate the price selected.
        // A fresh lookup or the mid rate would leave clean and dirty drifting apart by the spread.
        var bond = CreateBondValuation();

        var item = bond.Value(InstrumentPriceBasis.Bid, ValuationPriceConvention.Clean).Accounts.Single().Items.Single();

        Assert.Equal(AccruedInterestPerUnit, item.LocalAccruedInterest);
        Assert.Equal(AccruedInterestPerUnit * PositionQuantity * 0.75m, item.AccruedValue);
    }

    [Fact]
    public void Valuations_LeavesAccruedInterestNullWhereNothingAccrues()
    {
        // Ragged rows are the point: no zero accrual prints under an equity.
        var equity = CreateEquityValuation();

        var item = equity.Value(InstrumentPriceBasis.Mid, ValuationPriceConvention.Clean).Accounts.Single().Items.Single();

        Assert.Null(item.LocalAccruedInterest);
        Assert.Null(item.AccruedValue);
    }

    [Fact]
    public void Valuations_LeavesEquitiesUnchangedByTheConvention()
    {
        // Only fixed income accrues, so the convention must be inert everywhere else.
        var equity = CreateEquityValuation();

        var clean = equity.Value(InstrumentPriceBasis.Mid, ValuationPriceConvention.Clean);
        var dirty = equity.Value(InstrumentPriceBasis.Mid, ValuationPriceConvention.Dirty);

        Assert.Equal(clean.Totals.BookValue, dirty.Totals.BookValue);
    }

    private static ValuationFixture CreateBondValuation() =>
        CreateValuation(
            CreateInstrument(PositionInstrumentID, "Treasury 4%", "DBFUFR", "USD"),
            InstrumentPriceSetEventBuilder.CreateSeed(
                CreateEventID(),
                UserID,
                ValuationDate,
                AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(3)),
                "Set price",
                PositionInstrumentID,
                new InstrumentPriceFixedIncome(new InstrumentQuote(
                    new InstrumentPrice(CleanQuote(InstrumentPriceBasis.Bid)),
                    new InstrumentPrice(CleanQuote(InstrumentPriceBasis.Mid)),
                    new InstrumentPrice(CleanQuote(InstrumentPriceBasis.Ask))))).Value!,
            InstrumentIncomeSetEventBuilder.CreateSeed(
                CreateEventID(),
                UserID,
                ValuationDate,
                AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(3)),
                "Set income",
                PositionInstrumentID,
                new InstrumentIncomeFixedIncome(new InstrumentPrice(AccruedInterestPerUnit))).Value!,
            PositionInstrumentID,
            CreateUsdGbpRates());

    private static ValuationFixture CreateEquityValuation() =>
        CreateValuation(
            CreateInstrument(PositionInstrumentID, "First equity"),
            CreatePrice(PositionInstrumentID, 100m),
            CreateIncome(PositionInstrumentID),
            PositionInstrumentID,
            CreateUsdGbpRates());

    private static ValuationFixture CreateValuation(
        InstrumentCreatedEvent instrumentEvent,
        InstrumentPriceSetEvent priceEvent,
        InstrumentIncomeSetEvent incomeEvent,
        InstrumentID instrumentID,
        FXRates fxRates)
    {
        var accountID = AccountIDBuilder.Create();
        var holdingID = HoldingIDBuilder.Create();
        var outflowHoldingID = HoldingIDBuilder.Create();
        var accounts = CreateAccounts(accountID);
        var instrumentEvents = new IInstrumentEvent[] { instrumentEvent }.ToList();
        var instruments = new Instruments(ValuationDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(1)), instrumentEvents);
        var holdings = new Holdings(
            ValuationDate,
            AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(2)),
            new IHoldingEvent[]
            {
                CreateAssetHolding(holdingID, accountID, instrumentID, "Position"),
                CreateOutflowHolding(outflowHoldingID, accountID, instrumentID)
            }.ToList());
        var transactions = TransactionBuilder.Create(
            new TransactionSetRequest(
                UserID,
                ValuationDate,
                SettlementDateTimeBuilder.Create(ValuationDate.Value.AddDays(1)),
                "Book holding",
                [CreateTransactionLeg(holdingID, instrumentID, accountID, PositionQuantity, 800m)],
                [CreateTransactionLeg(outflowHoldingID, instrumentID, accountID, PositionQuantity, 800m)]),
            holdings).Value!.Cast<ITransactionEvent>().ToList();
        var asOfDate = AuditDateTimeBuilder.Create();
        var positions = new HoldingPositions(ValuationDate, asOfDate, holdings, accounts, instruments, transactions);
        var instrumentValues = new InstrumentValues(ValuationDate, asOfDate, instrumentEvents, [priceEvent], [incomeEvent]);

        return new ValuationFixture(accounts, positions, instrumentValues, fxRates, asOfDate);
    }

    private sealed record ValuationFixture(
        Accounts Accounts,
        HoldingPositions Positions,
        InstrumentValues InstrumentValues,
        FXRates FXRates,
        AuditDateTime AsOfDateTime)
    {
        public Valuations Value(InstrumentPriceBasis basis, ValuationPriceConvention convention) =>
            new(
                ValuationDate,
                AsOfDateTime,
                HoldingDateBasis.EventDateTime,
                basis,
                convention,
                Alpha3Builder.Create("GBP"),
                Accounts,
                Positions,
                InstrumentValues,
                FXRates);
    }

    private static FXRates CreateUsdGbpRates()
    {
        var usd = Alpha3Builder.Create("USD");
        var gbp = Alpha3Builder.Create("GBP");
        var fx = FXCreatedEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            AuditDate,
            "Create USD/GBP",
            usd,
            gbp,
            true).Value!;
        var rate = FXRateSetEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(1)),
            "Set USD/GBP",
            new CurrencyPair(usd, gbp),
            new FXPrice(new Bid(0.75m), new Mid(0.76m), new Ask(0.77m))).Value!;

        return new FXRates(ValuationDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(1)), [fx], [rate]);
    }

    private static decimal CleanQuote(InstrumentPriceBasis basis) =>
        basis switch
        {
            InstrumentPriceBasis.Bid => 99m,
            InstrumentPriceBasis.Ask => 101m,
            _ => 100m
        };

    private static Accounts CreateAccounts(AccountID accountID)
    {
        var created = AccountCreatedEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            AuditDate,
            "Create account",
            accountID,
            "General",
            "General Account",
            Alpha3Builder.Create("GBP"),
            true).Value!;

        return new Accounts(ValuationDate, AuditDate, [created]);
    }

    private static InstrumentCreatedEvent CreateInstrument(InstrumentID instrumentID, string name, string cfi = "ESVUFR", string priceCurrency = "GBP") =>
        InstrumentCreatedEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            AuditDate,
            "Create instrument",
            instrumentID,
            name,
            $"{name} plc",
            ExchangeBuilder.Create("XLON"),
            CFIBuilder.Create(cfi),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create(priceCurrency)).Value!;

    private static HoldingPositionAssetCreatedEvent CreateAssetHolding(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name) =>
        HoldingPositionAssetCreatedEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            AuditDate,
            "Create holding",
            holdingID,
            accountID,
            instrumentID,
            name,
            true,
            false).Value!;

    private static HoldingPositionOptionCreatedEvent CreateOptionHolding(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name) =>
        HoldingPositionOptionCreatedEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            AuditDate,
            "Create option holding",
            holdingID,
            accountID,
            instrumentID,
            name,
            true,
            false).Value!;

    private static HoldingNominalOutflowCreatedEvent CreateOutflowHolding(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID) =>
        HoldingNominalOutflowCreatedEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            AuditDate,
            "Create outflow",
            holdingID,
            accountID,
            instrumentID,
            "Outflow",
            true,
            false).Value!;

    private static InstrumentPriceSetEvent CreatePrice(InstrumentID instrumentID, decimal midPrice) =>
        InstrumentPriceSetEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(3)),
            "Set price",
            instrumentID,
            new InstrumentPriceEquity(
                new InstrumentQuote(new InstrumentPrice(midPrice), new InstrumentPrice(midPrice), new InstrumentPrice(midPrice)),
                new InstrumentPrice(midPrice),
                new InstrumentPrice(midPrice))).Value!;

    private static InstrumentIncomeSetEvent CreateIncome(InstrumentID instrumentID) =>
        InstrumentIncomeSetEventBuilder.CreateSeed(
            CreateEventID(),
            UserID,
            ValuationDate,
            AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(3)),
            "Set income",
            instrumentID,
            new InstrumentIncomeEquity(
                new InstrumentPrice(0m),
                "Regular",
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 1)),
                InstrumentDateBuilder.Create(new DateOnly(2025, 12, 1)),
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 2)),
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 31)))).Value!;

    private static TransactionRequest CreateTransactionLeg(HoldingID holdingID, InstrumentID instrumentID, AccountID accountID, decimal quantity, decimal bookCost) =>
        new(
            holdingID,
            instrumentID,
            accountID,
            new TransactionQuantity(quantity),
            new TransactionLocalCost(bookCost),
            Alpha3Builder.Create("GBP"),
            new TransactionBookCost(bookCost),
            BookCostSource.SameCurrency,
            false);

    private static readonly InstrumentID PositionInstrumentID = InstrumentIDBuilder.Create();
    private const decimal PositionQuantity = 10m;
    private const decimal AccruedInterestPerUnit = 1.25m;

    private static readonly UserID UserID = new(Guid.CreateGuid7());
    private static readonly EventDateTime ValuationDate = EventDateTimeBuilder.Create(new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(ValuationDate.Value.AddMinutes(1));

    private static EventID CreateEventID() => new(Guid.CreateGuid7());
}
