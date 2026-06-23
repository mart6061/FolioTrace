using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class ValuationBuilderTests
{
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

    private static InstrumentCreatedEvent CreateInstrument(InstrumentID instrumentID, string name) =>
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
            CFIBuilder.Create("ESVUFR"),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create("GBP")).Value!;

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
                new InstrumentPrice(midPrice),
                new InstrumentPrice(midPrice),
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

    private static readonly UserID UserID = new(Guid.CreateGuid7());
    private static readonly EventDateTime ValuationDate = EventDateTimeBuilder.Create(new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(ValuationDate.Value.AddMinutes(1));

    private static EventID CreateEventID() => new(Guid.CreateGuid7());
}
