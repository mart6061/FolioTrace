using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Test;

public sealed class HoldingBuilderTests
{
    [Fact]
    public void HoldingId_AcceptsNonEmptyGuid()
    {
        var holdingId = HoldingIDBuilder.Create();

        Assert.NotEqual(Guid.Empty, holdingId.Value);
    }

    [Fact]
    public void HoldingId_RejectsEmptyGuid() =>
        Assert.Throws<ArgumentException>(() => new HoldingID(Guid.Empty));

    [Fact]
    public void HoldingCreatedEventBuilder_RejectsMissingReferences()
    {
        var request = new HoldingCreatedRequest(UserID, EventDate, "Create holding", null, AccountIDBuilder.Create(), InstrumentIDBuilder.Create(), HoldingType.CashOnHand, null, "Capital", true, true);

        var result = HoldingCreatedEventBuilder.Create(request, CreateAccounts(), CreateInstruments(), null);

        Assert.False(result.IsValid);
        Assert.Contains("No matching Account found for AccountID", result.ValidationErrors[0]);
        Assert.Contains("No matching Instrument found for InstrumentID", result.ValidationErrors[1]);
    }

    [Fact]
    public void HoldingCreatedEventBuilder_RejectsDuplicateDefaultCashOnHand()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var holdings = CreateHoldings(accounts, instruments, CreateCashHolding(HoldingIDBuilder.Create(), true));
        var request = new HoldingCreatedRequest(UserID, EventDate, "Create holding", null, AccountID, CashInstrumentID, HoldingType.CashOnHand, null, "Income", true, true);

        var result = HoldingCreatedEventBuilder.Create(request, accounts, instruments, holdings);

        Assert.False(result.IsValid);
        Assert.Contains("A default CashOnHand holding already exists", result.ValidationErrors[0]);
    }

    [Fact]
    public void SeedData_CreatesCapitalAndIncomeCashOnHandHoldings()
    {
        var events = SeedRepository.CreateInitialHoldingCreatedEvents();

        Assert.Equal(40, events.Count);
        Assert.Equal(10, events.Count(@event => @event.HoldingType is HoldingType.CashOnHand && @event.Name == "Capital" && @event.Default));
        Assert.Equal(10, events.Count(@event => @event.HoldingType is HoldingType.CashOnHand && @event.Name == "Income" && !@event.Default));
        Assert.Equal(10, events.Count(@event => @event.HoldingType is HoldingType.Nominal && @event.NominalType is HoldingNominalType.Inflow));
        Assert.Equal(10, events.Count(@event => @event.HoldingType is HoldingType.Nominal && @event.NominalType is HoldingNominalType.Outflow));
    }

    [Fact]
    public void TransactionBuilder_RejectsInactiveAndMismatchedHoldings()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var inactiveHoldingID = HoldingIDBuilder.Create();
        var holdings = CreateHoldings(accounts, instruments, CreateCashHolding(inactiveHoldingID, false, active: false));
        var request = new TransactionSetRequest(
            UserID,
            EventDate,
            "Book transaction",
            [new TransactionRequest(inactiveHoldingID, EquityInstrumentID, AccountID, new TransactionQuantity(1m), new TransactionBookCost(10m))],
            [new TransactionRequest(inactiveHoldingID, CashInstrumentID, AccountID, new TransactionQuantity(1m), new TransactionBookCost(10m))]);

        var result = TransactionBuilder.Create(request, holdings);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, error => error.Contains("is inactive"));
        Assert.Contains(result.ValidationErrors, error => error.Contains("does not match InstrumentID"));
    }

    [Fact]
    public void HoldingPositions_AggregatesCashDepositAndInstrumentPurchase()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var cashHoldingID = HoldingIDBuilder.Create();
        var inflowHoldingID = HoldingIDBuilder.Create();
        var equityHoldingID = HoldingIDBuilder.Create();
        var holdings = CreateHoldings(
            accounts,
            instruments,
            CreateCashHolding(cashHoldingID, true),
            CreateNominalHolding(inflowHoldingID, HoldingNominalType.Inflow),
            CreatePositionHolding(equityHoldingID));
        var deposit = TransactionBuilder.Create(new TransactionSetRequest(
            UserID,
            EventDate,
            "Cash deposit",
            [new TransactionRequest(cashHoldingID, CashInstrumentID, AccountID, new TransactionQuantity(100m), new TransactionBookCost(50m))],
            [new TransactionRequest(inflowHoldingID, CashInstrumentID, AccountID, new TransactionQuantity(100m), new TransactionBookCost(50m))]), holdings).Value!;
        var purchase = TransactionBuilder.Create(new TransactionSetRequest(
            UserID,
            EventDateTimeBuilder.Create(EventDate.Value.AddTicks(1)),
            "Buy equity",
            [new TransactionRequest(equityHoldingID, EquityInstrumentID, AccountID, new TransactionQuantity(25m), new TransactionBookCost(50m))],
            [new TransactionRequest(cashHoldingID, CashInstrumentID, AccountID, new TransactionQuantity(50m), new TransactionBookCost(50m))]), holdings).Value!;
        var transactionEvents = deposit.Cast<ITransactionEvent>().Concat(purchase).ToList();

        var positions = new HoldingPositions(
            EventDateTimeBuilder.Create(EventDate.Value.AddDays(1)),
            AuditDateTimeBuilder.Create(DateTime.UtcNow),
            holdings,
            accounts,
            instruments,
            transactionEvents);

        Assert.Equal(2, positions.Items.Count);
        var cash = positions.Items.Single(position => position.HoldingID == cashHoldingID);
        Assert.Equal(50m, cash.Quantity);
        Assert.Equal(0m, cash.BookCost);
        var equity = positions.Items.Single(position => position.HoldingID == equityHoldingID);
        Assert.Equal(25m, equity.Quantity);
        Assert.Equal(50m, equity.BookCost);
    }

    [Fact]
    public void HoldingPositions_OmitsCancelledMovementsAtCancellationAuditTime()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var cashHoldingID = HoldingIDBuilder.Create();
        var inflowHoldingID = HoldingIDBuilder.Create();
        var holdings = CreateHoldings(accounts, instruments, CreateCashHolding(cashHoldingID, true), CreateNominalHolding(inflowHoldingID, HoldingNominalType.Inflow));
        var originalEvents = TransactionBuilder.Create(new TransactionSetRequest(
            UserID,
            EventDate,
            "Cash deposit",
            [new TransactionRequest(cashHoldingID, CashInstrumentID, AccountID, new TransactionQuantity(100m), new TransactionBookCost(100m))],
            [new TransactionRequest(inflowHoldingID, CashInstrumentID, AccountID, new TransactionQuantity(100m), new TransactionBookCost(100m))]), holdings).Value!;
        var cancellationEvents = TransactionCancellationEventBuilder.Create(new TransactionCancellationRequest(UserID, "Cancel", originalEvents[0].EventSetID), originalEvents.Cast<ITransactionEvent>().ToList()).Value!;
        var allEvents = originalEvents.Cast<ITransactionEvent>().Concat(cancellationEvents).ToList();

        var before = new HoldingPositions(EventDateTimeBuilder.Create(EventDate.Value.AddDays(1)), AuditDateTimeBuilder.Create(cancellationEvents[0].AuditDateTime.Value.AddTicks(-1)), holdings, accounts, instruments, allEvents);
        var after = new HoldingPositions(EventDateTimeBuilder.Create(EventDate.Value.AddDays(1)), cancellationEvents[0].AuditDateTime, holdings, accounts, instruments, allEvents);

        Assert.Single(before.Items);
        Assert.Empty(after.Items);
    }

    private static readonly UserID UserID = new(Guid.Parse("29133690-4018-43fb-b7f3-38108a755062"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-10));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-9));
    private static readonly AccountID AccountID = AccountIDBuilder.Create();
    private static readonly InstrumentID CashInstrumentID = InstrumentIDBuilder.Create();
    private static readonly InstrumentID EquityInstrumentID = InstrumentIDBuilder.Create();

    private static Accounts CreateAccounts()
    {
        var created = AccountCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            EventDate,
            AuditDate,
            "Create account",
            AccountID,
            "General",
            "General Account",
            Alpha3Builder.Create("GBP"),
            true).Value!;

        return new Accounts(EventDate, AuditDate, [created]);
    }

    private static Instruments CreateInstruments()
    {
        var cash = InstrumentCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            EventDate,
            AuditDate,
            "Create cash",
            CashInstrumentID,
            "British Pound Cash",
            "British Pound Cash",
            ExchangeBuilder.Create("CASH"),
            CFIBuilder.Create("MRCXXX"),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB")).Value!;
        var equity = InstrumentCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            EventDate,
            AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(1)),
            "Create equity",
            EquityInstrumentID,
            "Vodafone",
            "Vodafone Group plc",
            ExchangeBuilder.Create("XLON"),
            CFIBuilder.Create("ESVUFR"),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB")).Value!;

        return new Instruments(EventDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(1)), [cash, equity]);
    }

    private static Holdings CreateHoldings(Accounts accounts, Instruments instruments, params HoldingCreatedEvent[] holdingEvents) =>
        new(EventDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(holdingEvents.Length + 10)), holdingEvents.Cast<IHoldingEvent>().ToList());

    private static HoldingCreatedEvent CreateCashHolding(HoldingID holdingID, bool isDefault, bool active = true) =>
        CreateHolding(holdingID, CashInstrumentID, HoldingType.CashOnHand, null, isDefault ? "Capital" : "Income", active, isDefault);

    private static HoldingCreatedEvent CreateNominalHolding(HoldingID holdingID, HoldingNominalType nominalType) =>
        CreateHolding(holdingID, CashInstrumentID, HoldingType.Nominal, nominalType, nominalType.ToString(), true, false);

    private static HoldingCreatedEvent CreatePositionHolding(HoldingID holdingID) =>
        CreateHolding(holdingID, EquityInstrumentID, HoldingType.Position, null, string.Empty, true, false);

    private static HoldingCreatedEvent CreateHolding(HoldingID holdingID, InstrumentID instrumentID, HoldingType holdingType, HoldingNominalType? nominalType, string name, bool active, bool isDefault) =>
        HoldingCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            EventDate,
            AuditDate,
            "Create holding",
            holdingID,
            AccountID,
            instrumentID,
            holdingType,
            nominalType,
            name,
            active,
            isDefault).Value!;
}
