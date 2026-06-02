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
    public void HoldingPositionCashCreatedEventBuilder_RejectsMissingReferences()
    {
        var request = new HoldingPositionCashCreatedRequest(UserID, EventDate, "Create holding", null, AccountIDBuilder.Create(), InstrumentIDBuilder.Create(), "Capital", true, true);

        var result = HoldingPositionCashCreatedEventBuilder.Create(request, CreateAccounts(), CreateInstruments(), null);

        Assert.False(result.IsValid);
        Assert.Contains("No matching Account found for AccountID", result.ValidationErrors[0]);
        Assert.Contains("No matching Instrument found for InstrumentID", result.ValidationErrors[1]);
    }

    [Fact]
    public void HoldingCreatedEventBuilder_RejectsDuplicateDefaultForSameKind()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var holdings = CreateHoldings(accounts, instruments, CreateBankHolding(HoldingIDBuilder.Create(), typeof(HoldingCashInvestable), "Investable", isDefault: true));
        var request = new HoldingCashInvestableCreatedRequest(UserID, EventDate, "Create holding", null, AccountID, CashInstrumentID, "Reserve", true, true, "HSBC", "Reserve Account", SortCodeBuilder.Create("12-34-56"), BankAccountNumberBuilder.Create("12345678"), BICBuilder.Create("HBUKGB4B"), IBANBuilder.Create("GB82WEST12345698765432"));

        var result = HoldingCashInvestableCreatedEventBuilder.Create(request, accounts, instruments, holdings);

        Assert.False(result.IsValid);
        Assert.Contains("A default CashInvestable holding already exists", result.ValidationErrors[0]);
    }

    [Fact]
    public void HoldingCreatedEventBuilder_AllowsDefaultForDifferentKinds()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var holdings = CreateHoldings(accounts, instruments, CreateBankHolding(HoldingIDBuilder.Create(), typeof(HoldingCashDebt), "Debt", isDefault: true));
        var request = new HoldingCashInvestableCreatedRequest(UserID, EventDate, "Create holding", null, AccountID, CashInstrumentID, "Investable", true, true, "HSBC", "Investable Account", SortCodeBuilder.Create("12-34-56"), BankAccountNumberBuilder.Create("12345678"), BICBuilder.Create("HBUKGB4B"), IBANBuilder.Create("GB82WEST12345698765432"));

        var result = HoldingCashInvestableCreatedEventBuilder.Create(request, accounts, instruments, holdings);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void HoldingBankCreatedEventBuilder_RejectsMissingBankDetails()
    {
        var request = new HoldingCashDebtCreatedRequest(UserID, EventDate, "Create holding", null, AccountID, CashInstrumentID, "Debt", true, false, string.Empty, string.Empty, null!, null!, null!, null!);

        var result = HoldingCashDebtCreatedEventBuilder.Create(request, CreateAccounts(), CreateInstruments(), null);

        Assert.False(result.IsValid);
        Assert.Contains("BankName is required.", result.ValidationErrors);
        Assert.Contains("AccountName is required.", result.ValidationErrors);
        Assert.Contains("SortCode is required.", result.ValidationErrors);
        Assert.Contains("AccountNumber is required.", result.ValidationErrors);
        Assert.Contains("BIC is required.", result.ValidationErrors);
        Assert.Contains("IBAN is required.", result.ValidationErrors);
    }

    [Fact]
    public void BankIdentifierTypes_RejectInvalidValues()
    {
        Assert.Throws<ArgumentException>(() => SortCodeBuilder.Create("12345"));
        Assert.Throws<ArgumentException>(() => BankAccountNumberBuilder.Create("ABC12345"));
        Assert.Throws<ArgumentException>(() => BICBuilder.Create("BAD"));
        Assert.Throws<ArgumentException>(() => IBANBuilder.Create("GB00WEST12345698765432"));
    }

    [Fact]
    public void HoldingBankModifiedEventBuilder_UpdatesBankDetails()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var holdingID = HoldingIDBuilder.Create();
        var holdings = CreateHoldings(accounts, instruments, CreateBankHolding(holdingID, typeof(HoldingCashDebt), "Debt"));
        var request = new HoldingCashDebtModifiedRequest(UserID, EventDateTimeBuilder.Create(EventDate.Value.AddTicks(2)), "Modify holding", holdingID, "Debt reserve", false, "HSBC", "Reserve", SortCodeBuilder.Create("12-34-56"), BankAccountNumberBuilder.Create("12345678"), BICBuilder.Create("HBUKGB4B"), IBANBuilder.Create("GB82WEST12345698765432"));

        var result = HoldingCashDebtModifiedEventBuilder.Create(request, holdings);
        Assert.True(result.IsValid);

        holdings.Apply(result.Value!);
        var holding = Assert.IsType<HoldingCashDebt>(holdings.Items.Single(item => item.HoldingID == holdingID));
        Assert.Equal("Debt reserve", holding.Name);
        Assert.Equal("HSBC", holding.BankName);
        Assert.Equal("Reserve", holding.AccountName);
        Assert.Equal("12-34-56", holding.SortCode.Value);
        Assert.Equal("12345678", holding.AccountNumber.Value);
        Assert.Equal("HBUKGB4B", holding.BIC.Value);
        Assert.Equal("GB82WEST12345698765432", holding.IBAN.Value);
    }

    [Fact]
    public void SeedData_CreatesAccountHoldingsByType()
    {
        var events = SeedRepository.CreateInitialHoldingCreatedEvents();
        string[] custodianNames = ["Bank of New Year", "Royal Bank of Canada", "Bank of America"];
        string[] administratorNames = ["Capita", "Fundrock", "Gallium tailors"];
        string[] bankNames = ["HSBC", "Barclays"];
        string[] investableCurrencies = ["GBP", "EUR", "USD", "JPY"];

        Assert.Equal(208, events.Count);
        Assert.Equal(10, events.OfType<HoldingPositionCashCreatedEvent>().Count(@event => @event.Name == "Capital" && @event.Default));
        Assert.Equal(10, events.OfType<HoldingCashDebtCreatedEvent>().Count(@event => @event.Name == "Debt" && !@event.Default));
        Assert.Equal(40, events.OfType<HoldingCashInvestableCreatedEvent>().Count(@event => @event.Name.StartsWith("Investable ") && !@event.Default));
        Assert.Equal(10, events.OfType<HoldingCashNonInvestableCreatedEvent>().Count(@event => @event.Name == "Income" && !@event.Default));
        Assert.Equal(40, events.OfType<HoldingInflowCreatedEvent>().Count());
        Assert.Equal(40, events.OfType<HoldingOutflowCreatedEvent>().Count());
        Assert.All(investableCurrencies, currency =>
        {
            Assert.Equal(10, events.OfType<HoldingCashInvestableCreatedEvent>().Count(@event => @event.Name == $"Investable {currency}"));
            Assert.Equal(10, events.OfType<HoldingInflowCreatedEvent>().Count(@event => @event.Name == $"Inflow {currency}"));
            Assert.Equal(10, events.OfType<HoldingOutflowCreatedEvent>().Count(@event => @event.Name == $"Outflow {currency}"));
        });
        Assert.Equal(19, events.OfType<HoldingFeesCustodianCreatedEvent>().Count());
        Assert.Equal(9, events.OfType<HoldingFeesAdministratorCreatedEvent>().Count());
        Assert.Equal(10, events.OfType<HoldingFeesBankCreatedEvent>().Count());
        Assert.Equal(10, events.OfType<HoldingIncomeCreatedEvent>().Count());
        Assert.Equal(10, events.OfType<HoldingInterestCreatedEvent>().Count());
        Assert.All(events.OfType<HoldingBankCreatedEvent>(), @event =>
        {
            Assert.False(string.IsNullOrWhiteSpace(@event.BankName));
            Assert.False(string.IsNullOrWhiteSpace(@event.AccountName));
            Assert.False(string.IsNullOrWhiteSpace(@event.SortCode.Value));
            Assert.False(string.IsNullOrWhiteSpace(@event.AccountNumber.Value));
            Assert.False(string.IsNullOrWhiteSpace(@event.BIC.Value));
            Assert.False(string.IsNullOrWhiteSpace(@event.IBAN.Value));
        });
        Assert.All(events.OfType<HoldingFeesCustodianCreatedEvent>(), @event => Assert.Contains(@event.Name, custodianNames));
        Assert.All(events.OfType<HoldingFeesAdministratorCreatedEvent>(), @event => Assert.Contains(@event.Name, administratorNames));
        Assert.All(events.Where(@event => @event is HoldingFeesBankCreatedEvent or HoldingIncomeCreatedEvent or HoldingInterestCreatedEvent), @event => Assert.Contains(@event.Name, bankNames));
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
            SettlementDate,
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
            CreateNonValuationHolding(inflowHoldingID, typeof(HoldingInflow)),
            CreatePositionHolding(equityHoldingID));
        var deposit = TransactionBuilder.Create(new TransactionSetRequest(
            UserID,
            EventDate,
            SettlementDate,
            "Cash deposit",
            [new TransactionRequest(cashHoldingID, CashInstrumentID, AccountID, new TransactionQuantity(100m), new TransactionBookCost(50m))],
            [new TransactionRequest(inflowHoldingID, CashInstrumentID, AccountID, new TransactionQuantity(100m), new TransactionBookCost(50m))]), holdings).Value!;
        var purchaseDate = EventDateTimeBuilder.Create(EventDate.Value.AddTicks(1));
        var purchase = TransactionBuilder.Create(new TransactionSetRequest(
            UserID,
            purchaseDate,
            SettlementDateTimeBuilder.Create(purchaseDate.Value.AddDays(1)),
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
    public void HoldingPositions_UsesMarkerInterfacesForDefaultInclusion()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var cashDebtID = HoldingIDBuilder.Create();
        var cashInvestableID = HoldingIDBuilder.Create();
        var cashNonInvestableID = HoldingIDBuilder.Create();
        var inflowID = HoldingIDBuilder.Create();
        var outflowID = HoldingIDBuilder.Create();
        var holdings = CreateHoldings(
            accounts,
            instruments,
            CreateBankHolding(cashDebtID, typeof(HoldingCashDebt), "Debt"),
            CreateBankHolding(cashInvestableID, typeof(HoldingCashInvestable), "Investable"),
            CreateBankHolding(cashNonInvestableID, typeof(HoldingCashNonInvestable), "Income"),
            CreateNonValuationHolding(inflowID, typeof(HoldingInflow)),
            CreateNonValuationHolding(outflowID, typeof(HoldingOutflow)));
        var transactionEvents = TransactionBuilder.Create(new TransactionSetRequest(
            UserID,
            EventDate,
            SettlementDate,
            "Marker test",
            [
                new TransactionRequest(cashDebtID, CashInstrumentID, AccountID, new TransactionQuantity(1m), new TransactionBookCost(1m)),
                new TransactionRequest(cashInvestableID, CashInstrumentID, AccountID, new TransactionQuantity(2m), new TransactionBookCost(2m)),
                new TransactionRequest(cashNonInvestableID, CashInstrumentID, AccountID, new TransactionQuantity(3m), new TransactionBookCost(3m)),
                new TransactionRequest(inflowID, CashInstrumentID, AccountID, new TransactionQuantity(4m), new TransactionBookCost(4m))
            ],
            [new TransactionRequest(outflowID, CashInstrumentID, AccountID, new TransactionQuantity(10m), new TransactionBookCost(10m))]), holdings).Value!.Cast<ITransactionEvent>().ToList();

        var defaultPositions = new HoldingPositions(EventDateTimeBuilder.Create(EventDate.Value.AddDays(1)), AuditDateTimeBuilder.Create(DateTime.UtcNow), holdings, accounts, instruments, transactionEvents);
        var includedPositions = new HoldingPositions(EventDateTimeBuilder.Create(EventDate.Value.AddDays(1)), AuditDateTimeBuilder.Create(DateTime.UtcNow), holdings, accounts, instruments, transactionEvents, new HoldingPositionFilter(null, null, null, true, false));

        Assert.Equal(
            new[] { cashDebtID, cashInvestableID, cashNonInvestableID }.OrderBy(id => id.Value).ToArray(),
            defaultPositions.Items.Select(position => position.HoldingID).OrderBy(id => id.Value).ToArray());
        Assert.Contains(includedPositions.Items, position => position.HoldingID == inflowID && !position.IncludeInValuation);
    }

    [Fact]
    public void HoldingPositions_CanUseSettlementDateBasis()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var cashHoldingID = HoldingIDBuilder.Create();
        var inflowHoldingID = HoldingIDBuilder.Create();
        var holdings = CreateHoldings(accounts, instruments, CreateCashHolding(cashHoldingID, true), CreateNonValuationHolding(inflowHoldingID, typeof(HoldingInflow)));
        var transactionEvents = TransactionBuilder.Create(new TransactionSetRequest(
            UserID,
            EventDate,
            SettlementDate,
            "Cash deposit",
            [new TransactionRequest(cashHoldingID, CashInstrumentID, AccountID, new TransactionQuantity(100m), new TransactionBookCost(100m))],
            [new TransactionRequest(inflowHoldingID, CashInstrumentID, AccountID, new TransactionQuantity(100m), new TransactionBookCost(100m))]), holdings).Value!.Cast<ITransactionEvent>().ToList();
        var betweenExecutionAndSettlement = EventDateTimeBuilder.Create(EventDate.Value.AddHours(1));

        var executionPositions = new HoldingPositions(betweenExecutionAndSettlement, AuditDateTimeBuilder.Create(DateTime.UtcNow), holdings, accounts, instruments, transactionEvents, valuationDateBasis: ValuationDateBasis.EventDateTime);
        var settlementPositions = new HoldingPositions(betweenExecutionAndSettlement, AuditDateTimeBuilder.Create(DateTime.UtcNow), holdings, accounts, instruments, transactionEvents, valuationDateBasis: ValuationDateBasis.SettlementDateTime);
        var settledPositions = new HoldingPositions(EventDateTimeBuilder.Create(SettlementDate.Value), AuditDateTimeBuilder.Create(DateTime.UtcNow), holdings, accounts, instruments, transactionEvents, valuationDateBasis: ValuationDateBasis.SettlementDateTime);

        Assert.Single(executionPositions.Items);
        Assert.Empty(settlementPositions.Items);
        Assert.Single(settledPositions.Items);
        Assert.Equal(ValuationDateBasis.SettlementDateTime, settledPositions.ValuationDateBasis);
        Assert.All(settledPositions.Items, position => Assert.Equal(ValuationDateBasis.SettlementDateTime, position.ValuationDateBasis));
    }

    [Fact]
    public void HoldingPositions_OmitsCancelledMovementsAtCancellationAuditTime()
    {
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var cashHoldingID = HoldingIDBuilder.Create();
        var inflowHoldingID = HoldingIDBuilder.Create();
        var holdings = CreateHoldings(accounts, instruments, CreateCashHolding(cashHoldingID, true), CreateNonValuationHolding(inflowHoldingID, typeof(HoldingInflow)));
        var originalEvents = TransactionBuilder.Create(new TransactionSetRequest(
            UserID,
            EventDate,
            SettlementDate,
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
    private static readonly SettlementDateTime SettlementDate = SettlementDateTimeBuilder.Create(EventDate.Value.AddDays(1));
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
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create("GBP")).Value!;
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
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create("GBP")).Value!;

        return new Instruments(EventDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(1)), [cash, equity]);
    }

    private static Holdings CreateHoldings(Accounts accounts, Instruments instruments, params HoldingCreatedEvent[] holdingEvents) =>
        new(EventDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(holdingEvents.Length + 10)), holdingEvents.Cast<IHoldingEvent>().ToList());

    private static HoldingCreatedEvent CreateCashHolding(HoldingID holdingID, bool isDefault, bool active = true) =>
        isDefault
            ? HoldingPositionCashCreatedEventBuilder.CreateSeed(
                new EventID(Guid.NewGuid()),
                UserID,
                EventDate,
                AuditDate,
                "Create holding",
                holdingID,
                AccountID,
                CashInstrumentID,
                "Capital",
                active,
                true).Value!
            : CreateBankHolding(holdingID, typeof(HoldingCashNonInvestable), "Income", active);

    private static HoldingCreatedEvent CreateBankHolding(HoldingID holdingID, Type holdingType, string name, bool active = true, bool isDefault = false) =>
        holdingType.Name switch
        {
            nameof(HoldingCashDebt) => HoldingCashDebtCreatedEventBuilder.CreateSeed(
                new EventID(Guid.NewGuid()),
                UserID,
                EventDate,
                AuditDate,
                "Create holding",
                holdingID,
                AccountID,
                CashInstrumentID,
                name,
                active,
                isDefault,
                "HSBC",
                $"{name} Account",
                SortCodeBuilder.Create("12-34-56"),
                BankAccountNumberBuilder.Create("12345678"),
                BICBuilder.Create("HBUKGB4B"),
                IBANBuilder.Create("GB82WEST12345698765432")).Value!,
            nameof(HoldingCashInvestable) => HoldingCashInvestableCreatedEventBuilder.CreateSeed(
                new EventID(Guid.NewGuid()),
                UserID,
                EventDate,
                AuditDate,
                "Create holding",
                holdingID,
                AccountID,
                CashInstrumentID,
                name,
                active,
                isDefault,
                "HSBC",
                $"{name} Account",
                SortCodeBuilder.Create("12-34-56"),
                BankAccountNumberBuilder.Create("12345678"),
                BICBuilder.Create("HBUKGB4B"),
                IBANBuilder.Create("GB82WEST12345698765432")).Value!,
            nameof(HoldingCashNonInvestable) => HoldingCashNonInvestableCreatedEventBuilder.CreateSeed(
                new EventID(Guid.NewGuid()),
                UserID,
                EventDate,
                AuditDate,
                "Create holding",
                holdingID,
                AccountID,
                CashInstrumentID,
                name,
                active,
                isDefault,
                "HSBC",
                $"{name} Account",
                SortCodeBuilder.Create("12-34-56"),
                BankAccountNumberBuilder.Create("12345678"),
                BICBuilder.Create("HBUKGB4B"),
                IBANBuilder.Create("GB82WEST12345698765432")).Value!,
            _ => throw new ArgumentException($"Unsupported bank holding type '{holdingType}'.", nameof(holdingType))
        };

    private static HoldingCreatedEvent CreateNonValuationHolding(HoldingID holdingID, Type holdingType)
    {
        var name = HoldingKindRuntime.GetKindName(holdingType);

        return holdingType.Name switch
        {
            nameof(HoldingInflow) => HoldingInflowCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDate, "Create holding", holdingID, AccountID, CashInstrumentID, name, true, false).Value!,
            nameof(HoldingOutflow) => HoldingOutflowCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDate, "Create holding", holdingID, AccountID, CashInstrumentID, name, true, false).Value!,
            nameof(HoldingInspecieIn) => HoldingInspecieInCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDate, "Create holding", holdingID, AccountID, CashInstrumentID, name, true, false).Value!,
            nameof(HoldingInspecieOut) => HoldingInspecieOutCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDate, "Create holding", holdingID, AccountID, CashInstrumentID, name, true, false).Value!,
            nameof(HoldingFeesCustodian) => HoldingFeesCustodianCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDate, "Create holding", holdingID, AccountID, CashInstrumentID, name, true, false).Value!,
            nameof(HoldingFeesAdministrator) => HoldingFeesAdministratorCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDate, "Create holding", holdingID, AccountID, CashInstrumentID, name, true, false).Value!,
            nameof(HoldingFeesBank) => HoldingFeesBankCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDate, "Create holding", holdingID, AccountID, CashInstrumentID, name, true, false).Value!,
            nameof(HoldingIncome) => HoldingIncomeCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDate, "Create holding", holdingID, AccountID, CashInstrumentID, name, true, false).Value!,
            nameof(HoldingInterest) => HoldingInterestCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDate, "Create holding", holdingID, AccountID, CashInstrumentID, name, true, false).Value!,
            _ => throw new ArgumentException($"Unsupported nominal holding type '{holdingType}'.", nameof(holdingType))
        };
    }

    private static HoldingCreatedEvent CreatePositionHolding(HoldingID holdingID) =>
        HoldingPositionMemoCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            EventDate,
            AuditDate,
            "Create holding",
            holdingID,
            AccountID,
            EquityInstrumentID,
            string.Empty,
            true,
            false).Value!;

}
