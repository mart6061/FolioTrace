using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository.Seed;

namespace Repository;

public sealed class SeedRepository(IEventRepository eventRepository, IFXRateReadModelRepository fxRateReadModelRepository) : ISeedRepository
{
    private const int TotalBuildSteps = 16;
    private const int SeedTransactionMonths = 12;
    private const int SeedStocksPerAccount = 3;

    public async Task Build(CancellationToken cancellationToken = default)
    {
        await Build(null, cancellationToken);
    }

    public async Task Build(IProgress<BuildProgress>? progress, CancellationToken cancellationToken = default)
    {
        var buildProgress = CreateBuildProgress(progress, CountSeedEvents());

        await DeleteEvents(buildProgress, cancellationToken);
        await CreateSetupEvents(buildProgress, cancellationToken);
    }

    private async Task DeleteEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        progress("Clear", "Clearing events and projections.", 0, false);
        await eventRepository.ClearAsync(cancellationToken);
        await fxRateReadModelRepository.ClearAsync(cancellationToken);
        progress("Clear", "Events and projections cleared.", 0, true);
    }

    private async Task CreateSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        await CreateCountrySetupEvents(progress, cancellationToken);
        await CreateCurrencySetupEvents(progress, cancellationToken);
        await CreateBrokerSetupEvents(progress, cancellationToken);
        await CreateAccountSetupEvents(progress, cancellationToken);
        await CreateValuationSettingSetupEvents(progress, cancellationToken);
        await CreateReportSetupEvents(progress, cancellationToken);
        await CreateFXSetupEvents(progress, cancellationToken);
        await CreateInstrumentSetupEvents(progress, cancellationToken);
        await CreateHoldingSetupEvents(progress, cancellationToken);
        await CreateTransactionSetupEvents(progress, cancellationToken);
    }

    private async Task CreateCountrySetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var createdEvents = CreateInitialCountryCreatedEvents();
        var flagEvents = CreateInitialCountryFlagModifiedEvents();
        var modifiedEvents = CreateInitialCountryModifiedEvents();
        var eventCount = createdEvents.Count + flagEvents.Count + modifiedEvents.Count;

        progress("Countries", $"Seeding {eventCount:N0} country events.", 0, false);

        await StoreEvents<Countries, CountryCreatedEvent>(
            Constants.Initialisation.CountriesStreamId,
            createdEvents,
            cancellationToken);

        await AppendEvents(
            Constants.Initialisation.CountriesStreamId,
            flagEvents,
            cancellationToken);

        await AppendEvents(
            Constants.Initialisation.CountriesStreamId,
            modifiedEvents,
            cancellationToken);

        progress("Countries", $"Seeded {eventCount:N0} country events.", eventCount, true);
    }

    public static IReadOnlyList<CountryCreatedEvent> CreateInitialCountryCreatedEvents() =>
        SeedCountryCodes.Items
            .Select(country => CountryCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                Constants.Initialisation.EventDateTime,
                Constants.Initialisation.AuditDateTime,
                Constants.Initialisation.Reason,
                country.Alpha2,
                country.Alpha3,
                country.Numeric,
                country.Name).Value!)
            .ToList();

    public static IReadOnlyList<CountryFlagModifiedEvent> CreateInitialCountryFlagModifiedEvents()
    {
        var eventDateTime = EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(1));
        var auditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(1));

        return SeedCountryFlags.Items
            .Select(country => CountryFlagModifiedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                eventDateTime,
                auditDateTime,
                Constants.Initialisation.Reason,
                country.Alpha2,
                new CountryFlag(country.Svg)).Value!)
            .ToList();
    }

    public static IReadOnlyList<CountryModifiedEvent> CreateInitialCountryModifiedEvents()
    {
        var seed = SeedCountryCodes.Items.Single(country => country.Alpha2 == "AF");
        var names = new[]
        {
            "Afghanistan X",
            "Afghanistan AA",
            seed.Name
        };

        return names
            .Select((name, index) => CountryModifiedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(2 + index)),
                AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(2 + index)),
                Constants.Initialisation.Reason,
                seed.Alpha2,
                seed.Alpha3,
                seed.Numeric,
                name).Value!)
            .ToList();
    }

    private async Task CreateCurrencySetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var createdEvents = CreateInitialCurrencyCreatedEvents();
        var modifiedEvents = CreateInitialCurrencyModifiedEvents();
        var eventCount = createdEvents.Count + modifiedEvents.Count;

        progress("Currencies", $"Seeding {eventCount:N0} currency events.", 0, false);

        await StoreEvents<Currencies, CurrencyCreatedEvent>(
            Constants.Initialisation.CurrenciesStreamId,
            createdEvents,
            cancellationToken);

        await AppendEvents(
            Constants.Initialisation.CurrenciesStreamId,
            modifiedEvents,
            cancellationToken);

        progress("Currencies", $"Seeded {eventCount:N0} currency events.", eventCount, true);
    }

    public static IReadOnlyList<CurrencyCreatedEvent> CreateInitialCurrencyCreatedEvents() =>
        SeedCurrencyCodes.Items
            .Select(currency => CurrencyCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                Constants.Initialisation.EventDateTime,
                Constants.Initialisation.AuditDateTime,
                Constants.Initialisation.Reason,
                currency.AlphabeticCode,
                currency.NumericCode,
                currency.DecimalPlace,
                currency.Name).Value!)
            .ToList();

    public static IReadOnlyList<CurrencyModifiedEvent> CreateInitialCurrencyModifiedEvents()
    {
        var seed = SeedCurrencyCodes.Items.Single(currency => currency.AlphabeticCode == "EUR");
        var names = new[]
        {
            "Euro X",
            "Euro AA",
            seed.Name
        };

        return names
            .Select((name, index) => CurrencyModifiedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(2 + index)),
                AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(2 + index)),
                Constants.Initialisation.Reason,
                seed.AlphabeticCode,
                seed.NumericCode,
                seed.DecimalPlace,
                name).Value!)
            .ToList();
    }

    private static readonly (string Name, string LEI, decimal Commission, bool Active, DateTime ApprovedDateTime, DateTime NextReview, string Notes)[] SeedBrokers =
    [
        ("FoleoTrader", "FOLEOTRADER000000001", 0m, true, new DateTime(2024, 1, 2, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 1, 2, 9, 0, 0, DateTimeKind.Utc), "Automated FIX execution endpoint."),
        ("Northbridge Stockbrokers", "BROKERSEED0000000001", 0.0012m, true, new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 1, 15, 9, 0, 0, DateTimeKind.Utc), "Primary UK execution broker."),
        ("Harbourview Brokers", "BROKERSEED0000000002", 0.0010m, true, new DateTime(2024, 2, 20, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 2, 20, 9, 0, 0, DateTimeKind.Utc), "European equities coverage."),
        ("Cedar Lane Stockbrokers", "BROKERSEED0000000003", 0.0015m, true, new DateTime(2024, 3, 12, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 3, 12, 9, 0, 0, DateTimeKind.Utc), "Small and mid cap desk."),
        ("Meridian Brokers", "BROKERSEED0000000004", 0.0009m, true, new DateTime(2024, 4, 8, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 4, 8, 9, 0, 0, DateTimeKind.Utc), "Low touch electronic execution."),
        ("Kingsway Stockbrokers", "BROKERSEED0000000005", 0.0013m, true, new DateTime(2024, 5, 17, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 5, 17, 9, 0, 0, DateTimeKind.Utc), "UK retail execution venue access."),
        ("Albion Brokers", "BROKERSEED0000000006", 0.0011m, true, new DateTime(2024, 6, 5, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 6, 5, 9, 0, 0, DateTimeKind.Utc), "Sterling market coverage."),
        ("Summit Ridge Stockbrokers", "BROKERSEED0000000007", 0.0014m, true, new DateTime(2024, 7, 22, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 7, 22, 9, 0, 0, DateTimeKind.Utc), "Block execution support."),
        ("Greenfield Brokers", "BROKERSEED0000000008", 0.0008m, true, new DateTime(2024, 8, 14, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 8, 14, 9, 0, 0, DateTimeKind.Utc), "ETF and index flow."),
        ("Westhaven Stockbrokers", "BROKERSEED0000000009", 0.0016m, true, new DateTime(2024, 9, 3, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 9, 3, 9, 0, 0, DateTimeKind.Utc), "Special situations broker."),
        ("Sterling Gate Brokers", "BROKERSEED0000000010", 0.0010m, true, new DateTime(2024, 10, 11, 9, 0, 0, DateTimeKind.Utc), new DateTime(2027, 10, 11, 9, 0, 0, DateTimeKind.Utc), "International execution desk.")
    ];

    private async Task CreateBrokerSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var createdEvents = CreateInitialBrokerCreatedEvents();
        var eventCount = createdEvents.Count;

        progress("Brokers", $"Seeding {eventCount:N0} broker events.", 0, false);

        await StoreEvents<Brokers, BrokerCreatedEvent>(
            Constants.Initialisation.BrokersStreamId,
            createdEvents,
            cancellationToken);

        progress("Brokers", $"Seeded {eventCount:N0} broker events.", eventCount, true);
    }

    public static IReadOnlyList<BrokerCreatedEvent> CreateInitialBrokerCreatedEvents() =>
        SeedBrokers
            .Select((broker, index) => BrokerCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(10 + index)),
                AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(10 + index)),
                Constants.Initialisation.Reason,
                broker.Name,
                new LegalEntityIdentifier(broker.LEI),
                new FeeRate(broker.Commission),
                broker.Active,
                EventDateTimeBuilder.Create(broker.ApprovedDateTime),
                EventDateTimeBuilder.Create(broker.NextReview),
                broker.Notes).Value!)
            .ToList();

    private static readonly (AccountID AccountID, string Name, string FormalName, string BookCurrency, bool Active)[] SeedAccounts =
    [
        (AccountIDBuilder.Create(Guid.Parse("0d394930-9b8d-4f97-b358-52307f77bb7b")), "General Investment", "General Investment Account", "GBP", true),
        (AccountIDBuilder.Create(Guid.Parse("7894e034-2edc-4a92-aa71-1e58037d749c")), "ISA Growth", "Individual Savings Account", "GBP", true),
        (AccountIDBuilder.Create(Guid.Parse("4d76f43b-54a1-42b7-b6d9-39476f4b73af")), "SIPP Pension", "Self-Invested Personal Pension", "GBP", true),
        (AccountIDBuilder.Create(Guid.Parse("9f694e1d-f09b-4420-8e58-fdb46232e15e")), "US Broker", "United States Brokerage Account", "USD", true),
        (AccountIDBuilder.Create(Guid.Parse("5d3c708d-2551-4869-b081-15316b795a2f")), "Europe Broker", "European Brokerage Account", "EUR", true),
        (AccountIDBuilder.Create(Guid.Parse("df4b92d4-9960-4a4c-a5aa-a3a415b849da")), "Income Account", "Investment Income Account", "GBP", true),
        (AccountIDBuilder.Create(Guid.Parse("25b2a2f0-2d27-4473-9f30-5593f8795e2b")), "Treasury Cash", "Treasury Cash Account", "EUR", true),
        (AccountIDBuilder.Create(Guid.Parse("8b9c2940-670e-4ad7-ab4b-e132a469a486")), "Swiss Custody", "Swiss Custody Account", "CHF", true),
        (AccountIDBuilder.Create(Guid.Parse("9f96bef4-9806-4e31-b3af-d63bd3b70d21")), "Japan Custody", "Japan Custody Account", "JPY", true),
        (AccountIDBuilder.Create(Guid.Parse("38b8fdcb-b95e-4a44-a6cf-8bed4b9dbd52")), "Model Portfolio", "Model Portfolio Account", "GBP", true)
    ];

    private static readonly string[] SeedInvestableCashCurrencies = ["GBP", "EUR", "USD", "JPY", "CHF"];

    private async Task CreateAccountSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var createdEvents = CreateInitialAccountCreatedEvents();
        var modifiedEvents = CreateInitialAccountModifiedEvents();
        var activeEvents = CreateInitialAccountActiveModifiedEvents();
        var eventCount = createdEvents.Count + modifiedEvents.Count + activeEvents.Count;

        progress("Accounts", $"Seeding {eventCount:N0} account events.", 0, false);

        await StoreEvents<Accounts, AccountCreatedEvent>(
            Constants.Initialisation.AccountsStreamId,
            createdEvents,
            cancellationToken);

        await AppendEvents(
            Constants.Initialisation.AccountsStreamId,
            modifiedEvents,
            cancellationToken);

        await AppendEvents(
            Constants.Initialisation.AccountsStreamId,
            activeEvents,
            cancellationToken);

        progress("Accounts", $"Seeded {eventCount:N0} account events.", eventCount, true);
    }

    public static IReadOnlyList<AccountCreatedEvent> CreateInitialAccountCreatedEvents() =>
        SeedAccounts
            .Select((account, index) => AccountCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(10 + index)),
                AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(10 + index)),
                Constants.Initialisation.Reason,
                account.AccountID,
                account.Name,
                account.FormalName,
                Alpha3Builder.Create(account.BookCurrency),
                account.Active).Value!)
            .ToList();

    public static IReadOnlyList<AccountModifiedEvent> CreateInitialAccountModifiedEvents()
    {
        var modifications = new[]
        {
            (SeedAccounts[1].AccountID, "ISA Growth Portfolio", "Individual Savings Account Growth Portfolio"),
            (SeedAccounts[3].AccountID, "US Trading Account", "United States Trading Account"),
            (SeedAccounts[6].AccountID, "Treasury Reserve", "Treasury Reserve Account")
        };

        return modifications
            .Select((account, index) => AccountModifiedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(30 + index)),
                AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(30 + index)),
                Constants.Initialisation.Reason,
                account.AccountID,
                account.Item2,
                account.Item3).Value!)
            .ToList();
    }

    public static IReadOnlyList<AccountActiveSetEvent> CreateInitialAccountActiveModifiedEvents()
    {
        var activeChanges = new[]
        {
            (SeedAccounts[4].AccountID, false),
            (SeedAccounts[4].AccountID, true),
            (SeedAccounts[8].AccountID, false)
        };

        return activeChanges
            .Select((account, index) => AccountActiveSetEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(40 + index)),
                AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(40 + index)),
                Constants.Initialisation.Reason,
                account.AccountID,
                account.Item2).Value!)
            .ToList();
    }

    private async Task CreateValuationSettingSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var createdEvents = CreateInitialAssetAllocationCreatedEvents();
        var eventCount = createdEvents.Count;

        progress("Asset allocation tools", $"Seeding {eventCount:N0} asset allocation configuration events.", 0, false);

        await StoreEvents<ValuationSettings, AssetAllocationCreatedEvent>(
            Constants.Initialisation.ValuationSettingsStreamId,
            createdEvents,
            cancellationToken);

        progress("Asset allocation tools", $"Seeded {eventCount:N0} asset allocation configuration events.", eventCount, true);
    }

    public static IReadOnlyList<AssetAllocationCreatedEvent> CreateInitialAssetAllocationCreatedEvents()
    {
        var assetAllocationID = AssetAllocationIDBuilder.Create(CreateDeterministicGuid("asset-allocation-current"));
        var rootNodeID = NodeIDBuilder.Create(CreateDeterministicGuid("asset-allocation-current-root"));
        var unallocatedNodeID = NodeIDBuilder.Create(CreateDeterministicGuid("asset-allocation-current-unallocated"));
        var growthNodeID = NodeIDBuilder.Create(CreateDeterministicGuid("asset-allocation-current-growth"));
        var internationalNodeID = NodeIDBuilder.Create(CreateDeterministicGuid("asset-allocation-current-international"));
        var cashIncomeNodeID = NodeIDBuilder.Create(CreateDeterministicGuid("asset-allocation-current-cash-income"));
        var accountIDs = SeedAccounts.Select(account => account.AccountID).ToList();
        var eventDateTime = EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(45));
        var auditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(45));

        return
        [
            AssetAllocationCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                eventDateTime,
                eventDateTime,
                auditDateTime,
                Constants.Initialisation.Reason,
                assetAllocationID,
                "Current",
                accountIDs,
                true,
                rootNodeID,
                [
                    new AssetAllocationNode(rootNodeID, [unallocatedNodeID, growthNodeID, internationalNodeID, cashIncomeNodeID], "Current", false, true, [], "#0f766e"),
                    new AssetAllocationNode(unallocatedNodeID, [], "Unallocated", false, false, [], "#dc2626"),
                    new AssetAllocationNode(
                        growthNodeID,
                        [],
                        "Growth",
                        false,
                        false,
                        CreateSeedAllocationSettings(
                            (SeedAccounts[0].AccountID, 0.60m, 0.70m, 0.50m, 0.02m),
                            (SeedAccounts[1].AccountID, 0.80m, 0.90m, 0.70m, 0.01m),
                            (SeedAccounts[2].AccountID, 0.70m, 0.80m, 0.60m, 0.02m),
                            (SeedAccounts[9].AccountID, 0.65m, 0.75m, 0.55m, 0.02m)),
                        "#0f766e"),
                    new AssetAllocationNode(
                        internationalNodeID,
                        [],
                        "International",
                        false,
                        false,
                        CreateSeedAllocationSettings(
                            (SeedAccounts[3].AccountID, 0.75m, 0.85m, 0.65m, 0.01m),
                            (SeedAccounts[4].AccountID, 0.70m, 0.80m, 0.60m, 0.02m),
                            (SeedAccounts[7].AccountID, 0.55m, 0.65m, 0.45m, 0.01m),
                            (SeedAccounts[8].AccountID, 0.50m, 0.60m, 0.40m, 0.01m)),
                        "#2563eb"),
                    new AssetAllocationNode(
                        cashIncomeNodeID,
                        [],
                        "Cash and Income",
                        false,
                        false,
                        CreateSeedAllocationSettings(
                            (SeedAccounts[5].AccountID, 0.85m, 0.95m, 0.75m, 0.04m),
                            (SeedAccounts[6].AccountID, 0.90m, 1.00m, 0.80m, 0.03m)),
                        "#7c3aed")
                ]).Value!
        ];
    }

    private static List<AssetAllocationNodeAccountSetting> CreateSeedAllocationSettings(params (AccountID AccountID, decimal TargetWeight, decimal TargetWeightMax, decimal TargetWeightMin, decimal TargetYield)[] settings) =>
        settings
            .Select(setting => new AssetAllocationNodeAccountSetting(setting.AccountID, setting.TargetWeight, setting.TargetWeightMax, setting.TargetWeightMin, setting.TargetYield))
            .ToList();

    private async Task CreateReportSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var createdEvents = CreateInitialReportCreatedEvents();
        var eventCount = createdEvents.Count;

        progress("Report tools", $"Seeding {eventCount:N0} report configuration events.", 0, false);

        await StoreEvents<ReportConfigs, ReportCreatedEvent>(
            Constants.Initialisation.ReportConfigsStreamId,
            createdEvents,
            cancellationToken);

        progress("Report tools", $"Seeded {eventCount:N0} report configuration events.", eventCount, true);
    }

    public static IReadOnlyList<ReportCreatedEvent> CreateInitialReportCreatedEvents()
    {
        var allocationEvents = CreateInitialAssetAllocationCreatedEvents();
        var valuationSettings = new ValuationSettings(
            EventDateTimeBuilder.Create(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            allocationEvents.Cast<IValuationSettingEvent>().ToList());
        var assetAllocationID = valuationSettings.Items.Single(setting => setting.Name == "Current").AssetAllocationID;
        var reportID = ReportIDBuilder.Create(CreateDeterministicGuid("report-current"));
        var eventDateTime = EventDateTimeBuilder.Create(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var auditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(46));

        return
        [
            ReportCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                eventDateTime,
                auditDateTime,
                Constants.Initialisation.Reason,
                reportID,
                "Current",
                true,
                eventDateTime,
                CreateCurrentReportNodes(assetAllocationID),
                valuationSettings: valuationSettings).Value!
        ];
    }

    private static List<ReportNodeBase> CreateCurrentReportNodes(AssetAllocationID assetAllocationID) =>
    [
        new ReportNodeCoverPage(
            ReportNodeIDBuilder.Create(CreateDeterministicGuid("report-current-cover-page")),
            1,
            "Cover Page",
            "Cover Page"),
        new ReportNodeIndex(
            ReportNodeIDBuilder.Create(CreateDeterministicGuid("report-current-index")),
            2,
            "Index",
            "Index"),
        new ReportNodeChart(
            ReportNodeIDBuilder.Create(CreateDeterministicGuid("report-current-asset-allocation-chart")),
            3,
            "Chart",
            "Asset Allocation Chart",
            assetAllocationID,
            ReportChartType.Pie),
        new ReportNodeValuation(
            ReportNodeIDBuilder.Create(CreateDeterministicGuid("report-current-valuation")),
            4,
            "Valuation",
            "Valuation",
            assetAllocationID,
            ReportConfigBuilder.DefaultValuationColumns()) { PageOrientation = ReportNodePageOrientation.Landscape },
        new ReportNodeTransactions(
            ReportNodeIDBuilder.Create(CreateDeterministicGuid("report-current-transactions")),
            5,
            "Transactions",
            "Transactions",
            assetAllocationID) { PageOrientation = ReportNodePageOrientation.Landscape },
        new ReportNodeCash(
            ReportNodeIDBuilder.Create(CreateDeterministicGuid("report-current-cash")),
            6,
            "Cash",
            "Cash",
            assetAllocationID) { PageOrientation = ReportNodePageOrientation.Landscape }
    ];

    private async Task CreateHoldingSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var instrumentSeeds = SeedInstrumentData.CreateInstrumentSeeds();
        var createdEvents = CreateInitialHoldingCreatedEvents(instrumentSeeds);
        var eventCount = createdEvents.Count;

        progress("Holdings", $"Seeding {eventCount:N0} holding events.", 0, false);

        await StoreEvents<Holdings, HoldingCreatedEvent>(
            Constants.Initialisation.HoldingsStreamId,
            createdEvents,
            cancellationToken);

        progress("Holdings", $"Seeded {eventCount:N0} holding events.", eventCount, true);
    }

    public static IReadOnlyList<HoldingCreatedEvent> CreateInitialHoldingCreatedEvents() =>
        CreateInitialHoldingCreatedEvents(SeedInstrumentData.CreateInstrumentSeeds());

    private static IReadOnlyList<HoldingCreatedEvent> CreateInitialHoldingCreatedEvents(IReadOnlyList<InstrumentSeed> instrumentSeeds)
    {
        var events = new List<HoldingCreatedEvent>();
        var index = 0;

        foreach (var account in SeedAccounts)
        {
            var cashInstrument = instrumentSeeds.Single(seed => seed.Kind is InstrumentSeedKind.Cash && seed.Currency == account.BookCurrency);
            var accountIndex = Array.FindIndex(SeedAccounts, seed => seed.AccountID == account.AccountID);

            events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrument.InstrumentID, typeof(HoldingPositionCash), "Capital", true, true));
            events.Add(CreateSeedBankHolding(index++, account.AccountID, cashInstrument.InstrumentID, typeof(HoldingCashDebt), "Debt", true, false, account, accountIndex));
            events.Add(CreateSeedBankHolding(index++, account.AccountID, cashInstrument.InstrumentID, typeof(HoldingCashNonInvestable), "Income", true, false, account, accountIndex));

            foreach (var investableCashInstrument in SeedInvestableCashCurrencies
                .Select(currency => instrumentSeeds.Single(seed => seed.Kind is InstrumentSeedKind.Cash && seed.Currency == currency)))
            {
                events.Add(CreateSeedBankHolding(index++, account.AccountID, investableCashInstrument.InstrumentID, typeof(HoldingCashInvestable), $"Investable {investableCashInstrument.Currency}", true, false, account, accountIndex));
                events.Add(CreateSeedHolding(index++, account.AccountID, investableCashInstrument.InstrumentID, typeof(HoldingInflow), $"Inflow {investableCashInstrument.Currency}", true, false));
                events.Add(CreateSeedHolding(index++, account.AccountID, investableCashInstrument.InstrumentID, typeof(HoldingOutflow), $"Outflow {investableCashInstrument.Currency}", true, false));
            }

            AddNonValuationSeedHoldings(events, ref index, account, cashInstrument.InstrumentID);

            foreach (var equity in SelectSeedEquitiesForAccount(instrumentSeeds, accountIndex))
            {
                events.Add(CreateSeedHolding(index++, account.AccountID, equity.InstrumentID, typeof(HoldingPositionAsset), $"Asset {equity.Ticker}", true, false));
                events.Add(CreateSeedHolding(index++, account.AccountID, equity.InstrumentID, typeof(HoldingInSpecieIn), $"InSpecie In {equity.Ticker}", true, false));
                events.Add(CreateSeedHolding(index++, account.AccountID, equity.InstrumentID, typeof(HoldingInSpecieOut), $"InSpecie Out {equity.Ticker}", true, false));
            }
        }

        return events;
    }

    private async Task CreateTransactionSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var transactionEvents = CreateInitialTransactionEvents();

        progress("Transactions", $"Seeding {transactionEvents.Count:N0} transaction events.", 0, false);

        var storedEvents = 0;
        await AppendEventsInBatches(
            Constants.Initialisation.TransactionsStreamId,
            transactionEvents,
            cancellationToken,
            eventCount =>
            {
                storedEvents += eventCount;
                progress("Transactions", $"Seeded {storedEvents:N0} of {transactionEvents.Count:N0} transaction events.", eventCount, false);
            });

        progress("Transactions", $"Seeded {transactionEvents.Count:N0} transaction events.", 0, true);
    }

    public static IReadOnlyList<ITransactionEvent> CreateInitialTransactionEvents()
    {
        var instrumentSeeds = SeedInstrumentData.CreateInstrumentSeeds();
        var holdingEvents = CreateInitialHoldingCreatedEvents(instrumentSeeds);
        return CreateInitialTransactionEvents(instrumentSeeds, holdingEvents);
    }

    private static IReadOnlyList<ITransactionEvent> CreateInitialTransactionEvents(IReadOnlyList<InstrumentSeed> instrumentSeeds, IReadOnlyList<HoldingCreatedEvent> holdingEvents)
    {
        var holdings = new Holdings(
            EventDateTimeBuilder.Create(DateTime.UtcNow),
            AuditDateTimeBuilder.Create(),
            holdingEvents.Cast<IHoldingEvent>().ToList());
        var events = new List<ITransactionEvent>();
        var startDate = DateTime.UtcNow.Date.AddYears(-1);
        var endDate = startDate.AddYears(1);

        foreach (var account in SeedAccounts)
        {
            var accountIndex = Array.FindIndex(SeedAccounts, seed => seed.AccountID == account.AccountID);
            var random = CreateSeedRandom(account.AccountID);
            var cashInstrument = instrumentSeeds.Single(seed => seed.Kind is InstrumentSeedKind.Cash && seed.Currency == account.BookCurrency);
            var cashHolding = FindSeedHolding(holdings, account.AccountID, cashInstrument.InstrumentID, typeof(HoldingCashInvestable), $"Investable {account.BookCurrency}");
            var inflowHolding = FindSeedHolding(holdings, account.AccountID, cashInstrument.InstrumentID, typeof(HoldingInflow), $"Inflow {account.BookCurrency}");
            var outflowHolding = FindSeedHolding(holdings, account.AccountID, cashInstrument.InstrumentID, typeof(HoldingOutflow), $"Outflow {account.BookCurrency}");
            var selectedEquities = SelectSeedEquitiesForAccount(instrumentSeeds, accountIndex);
            var positionStates = selectedEquities.ToDictionary(equity => equity.InstrumentID, _ => new SeedPositionState());
            var previousInflow = RoundMoney(random.Next(10_000, 2_000_001));

            for (var month = 0; month < SeedTransactionMonths; month++)
            {
                var monthDate = startDate.AddMonths(month);
                var monthEnd = monthDate.AddMonths(1).AddDays(-1);
                var inflowAmount = month == 0
                    ? previousInflow
                    : RoundMoney(Math.Max(100m, previousInflow * (decimal)(random.NextDouble() * 2d)));
                previousInflow = inflowAmount;

                events.AddRange(CreateSeedTransactionSet(
                    holdings,
                    cashHolding,
                    inflowHolding,
                    cashInstrument.InstrumentID,
                    inflowAmount,
                    inflowAmount,
                    monthDate.AddHours(9),
                    $"Seed monthly cash in {month + 1}"));

                var outflowAmount = RoundMoney(inflowAmount * (0.01m + ((decimal)random.NextDouble() * 0.04m)));
                events.AddRange(CreateSeedTransactionSet(
                    holdings,
                    outflowHolding,
                    cashHolding,
                    cashInstrument.InstrumentID,
                    outflowAmount,
                    outflowAmount,
                    monthDate.AddDays(2).AddHours(9),
                    $"Seed monthly cash out {month + 1}"));

                for (var weekDate = startDate; weekDate < endDate; weekDate = weekDate.AddDays(7))
                {
                    if (weekDate < monthDate || weekDate >= monthDate.AddMonths(1))
                        continue;

                    foreach (var equity in selectedEquities.Select((seed, index) => (Seed: seed, Index: index)))
                    {
                        var bookCost = RoundMoney(inflowAmount * 0.10m);
                        var quantity = RoundQuantity(bookCost / equity.Seed.BasePrice);
                        if (quantity <= 0m || bookCost <= 0m)
                            continue;

                        var assetHolding = FindSeedHolding(holdings, account.AccountID, equity.Seed.InstrumentID, typeof(HoldingPositionAsset), $"Asset {equity.Seed.Ticker}");
                        var inSpecieInHolding = FindSeedHolding(holdings, account.AccountID, equity.Seed.InstrumentID, typeof(HoldingInSpecieIn), $"InSpecie In {equity.Seed.Ticker}");
                        var positionState = positionStates[equity.Seed.InstrumentID];

                        events.AddRange(CreateSeedTransactionSet(
                            holdings,
                            assetHolding,
                            inSpecieInHolding,
                            equity.Seed.InstrumentID,
                            quantity,
                            bookCost,
                            weekDate.AddHours(10 + equity.Index),
                            $"Seed weekly InSpecie in {equity.Seed.Ticker}"));

                        positionState.Quantity += quantity;
                        positionState.BookCost += bookCost;
                    }
                }

                var outEquity = selectedEquities[(accountIndex + month) % selectedEquities.Count];
                var state = positionStates[outEquity.InstrumentID];
                var outQuantity = RoundQuantity(state.Quantity * 0.25m);
                var outBookCost = RoundMoney(state.BookCost * 0.25m);
                if (outQuantity <= 0m || outBookCost <= 0m)
                    continue;

                var outAssetHolding = FindSeedHolding(holdings, account.AccountID, outEquity.InstrumentID, typeof(HoldingPositionAsset), $"Asset {outEquity.Ticker}");
                var inSpecieOutHolding = FindSeedHolding(holdings, account.AccountID, outEquity.InstrumentID, typeof(HoldingInSpecieOut), $"InSpecie Out {outEquity.Ticker}");

                events.AddRange(CreateSeedTransactionSet(
                    holdings,
                    inSpecieOutHolding,
                    outAssetHolding,
                    outEquity.InstrumentID,
                    outQuantity,
                    outBookCost,
                    monthEnd.AddHours(15),
                    $"Seed monthly InSpecie out {outEquity.Ticker}"));

                state.Quantity -= outQuantity;
                state.BookCost -= outBookCost;
            }
        }

        return events
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();
    }

    private static IReadOnlyList<ITransactionEvent> CreateSeedTransactionSet(Holdings holdings, Holding creditHolding, Holding debitHolding, InstrumentID instrumentID, decimal quantity, decimal bookCost, DateTime eventDateTime, string reason)
    {
        var request = new TransactionSetRequest(
            Constants.Initialisation.UserID,
            EventDateTimeBuilder.Create(eventDateTime),
            SettlementDateTimeBuilder.Create(eventDateTime.AddDays(2)),
            reason,
            [new TransactionRequest(creditHolding.HoldingID, instrumentID, creditHolding.AccountID, new TransactionQuantity(quantity), new TransactionBookCost(bookCost))],
            [new TransactionRequest(debitHolding.HoldingID, instrumentID, debitHolding.AccountID, new TransactionQuantity(quantity), new TransactionBookCost(bookCost))]);
        var result = TransactionBuilder.Create(request, holdings);

        if (result.IsValid && result.Value is not null)
            return result.Value.Cast<ITransactionEvent>().ToList();

        throw new InvalidOperationException($"Unable to create seed transaction '{reason}': {string.Join("; ", result.ValidationErrors)}");
    }

    private static Holding FindSeedHolding(Holdings holdings, AccountID accountID, InstrumentID instrumentID, Type holdingType, string name)
    {
        var holdingKind = HoldingKindRuntime.GetKindName(holdingType);
        return holdings.Items.Single(holding =>
            holding.AccountID == accountID
            && holding.InstrumentID == instrumentID
            && holding.GetHoldingKindName() == holdingKind
            && holding.Name == name);
    }

    private static IReadOnlyList<InstrumentSeed> SelectSeedEquitiesForAccount(IReadOnlyList<InstrumentSeed> instrumentSeeds, int accountIndex)
    {
        var equities = instrumentSeeds
            .Where(seed => seed.Kind is InstrumentSeedKind.Equity)
            .OrderBy(seed => seed.Ticker, StringComparer.Ordinal)
            .ToList();
        var selected = new List<InstrumentSeed>();
        var offset = Math.Abs(accountIndex * 7) % equities.Count;

        for (var index = 0; selected.Count < SeedStocksPerAccount; index++)
        {
            var equity = equities[(offset + (index * 11)) % equities.Count];
            if (!selected.Any(seed => seed.InstrumentID == equity.InstrumentID))
                selected.Add(equity);
        }

        return selected;
    }

    private static Random CreateSeedRandom(AccountID accountID)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"seed-transactions-{accountID.Value}"));
        return new Random(BitConverter.ToInt32(bytes, 0) & int.MaxValue);
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal RoundQuantity(decimal value) => Math.Round(value, 8, MidpointRounding.AwayFromZero);

    private static void AddNonValuationSeedHoldings(List<HoldingCreatedEvent> events, ref int index, (AccountID AccountID, string Name, string FormalName, string BookCurrency, bool Active) account, InstrumentID cashInstrumentID)
    {
        var accountIndex = Array.FindIndex(SeedAccounts, seed => seed.AccountID == account.AccountID);
        var custodianNames = new[] { "Bank of New Year", "Royal Bank of Canada", "Bank of America" };
        var administratorNames = new[] { "Capita", "Fundrock", "Gallium tailors" };
        var bankNames = new[] { "HSBC", "Barclays" };

        foreach (var name in custodianNames.Take(1 + accountIndex % custodianNames.Length))
            events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrumentID, typeof(HoldingFeesCustodian), name, true, false));

        foreach (var name in administratorNames.Take(accountIndex % administratorNames.Length))
            events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrumentID, typeof(HoldingFeesAdministrator), name, true, false));

        events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrumentID, typeof(HoldingFeesBank), bankNames[accountIndex % bankNames.Length], true, false));
        events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrumentID, typeof(HoldingIncome), bankNames[(accountIndex + 1) % bankNames.Length], true, false));
        events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrumentID, typeof(HoldingInterest), bankNames[accountIndex % bankNames.Length], true, false));
    }

    private static HoldingCreatedEvent CreateSeedHolding(int index, AccountID accountID, InstrumentID instrumentID, Type holdingType, string name, bool active, bool isDefault)
    {
        var eventId = new EventID(Guid.CreateGuid7());
        var eventDateTime = EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(50 + index));
        var auditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(50 + index));
        var holdingKind = HoldingKindRuntime.GetKindName(holdingType);
        var holdingID = HoldingIDBuilder.Create(CreateDeterministicGuid($"holding-{accountID}-{instrumentID}-{holdingKind}-{name}"));

        return holdingType.Name switch
        {
            nameof(HoldingPositionMemo) => HoldingPositionMemoCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingPositionCash) => HoldingPositionCashCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingPositionAsset) => HoldingPositionAssetCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingInflow) => HoldingInflowCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingOutflow) => HoldingOutflowCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingInSpecieIn) => HoldingInSpecieInCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingInSpecieOut) => HoldingInSpecieOutCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingFeesCustodian) => HoldingFeesCustodianCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingFeesAdministrator) => HoldingFeesAdministratorCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingFeesBank) => HoldingFeesBankCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingIncome) => HoldingIncomeCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingInterest) => HoldingInterestCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            _ => throw new InvalidOperationException($"Unsupported seed holding kind '{holdingKind}'.")
        };
    }

    private static HoldingCreatedEvent CreateSeedBankHolding(int index, AccountID accountID, InstrumentID instrumentID, Type holdingType, string name, bool active, bool isDefault, (AccountID AccountID, string Name, string FormalName, string BookCurrency, bool Active) account, int accountIndex)
    {
        var eventId = new EventID(Guid.CreateGuid7());
        var eventDateTime = EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(50 + index));
        var auditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(50 + index));
        var holdingKind = HoldingKindRuntime.GetKindName(holdingType);
        var holdingID = HoldingIDBuilder.Create(CreateDeterministicGuid($"holding-{accountID}-{instrumentID}-{holdingKind}-{name}"));
        var bankNames = new[] { "HSBC", "Barclays" };
        var bankName = bankNames[(accountIndex + name.Length) % bankNames.Length];
        var accountName = $"{account.Name} {name}";
        var sortCode = SortCodeBuilder.Create($"{accountIndex + 10:00}-{index % 100:00}-{name.Length % 100:00}");
        var accountNumber = BankAccountNumberBuilder.Create($"{10000000 + index:00000000}");
        var bic = BICBuilder.Create(bankName == "Barclays" ? "BARCGB22" : "HBUKGB4B");
        var iban = IBANBuilder.Create("GB82WEST12345698765432");

        return holdingType.Name switch
        {
            nameof(HoldingCashDebt) => HoldingCashDebtCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban).Value!,
            nameof(HoldingCashInvestable) => HoldingCashInvestableCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban).Value!,
            nameof(HoldingCashNonInvestable) => HoldingCashNonInvestableCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault, bankName, accountName, sortCode, accountNumber, bic, iban).Value!,
            _ => throw new InvalidOperationException($"Unsupported seed bank holding kind '{holdingKind}'.")
        };
    }

    private async Task CreateFXSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var pairSeeds = SeedFXData.CreatePairSeeds();
        var fxEvents = CreateInitialFXCreatedEvents(pairSeeds);
        var rateEvents = CreateInitialFXRateSetEvents(pairSeeds).ToList();

        progress("FX definitions", $"Seeding {fxEvents.Count:N0} FX definition events.", 0, false);

        await StoreEvents<FXs, FXCreatedEvent>(
            Constants.Initialisation.FXsStreamId,
            fxEvents,
            cancellationToken);

        progress("FX definitions", $"Seeded {fxEvents.Count:N0} FX definition events.", fxEvents.Count, true);
        progress("FX rates", $"Seeding {rateEvents.Count:N0} FX rate events.", 0, false);

        var storedRateEvents = 0;
        await StoreEventsInBatches<FXRates, FXRateSetEvent>(
            Constants.Initialisation.FXRatesStreamId,
            rateEvents,
            cancellationToken,
            eventCount =>
            {
                storedRateEvents += eventCount;
                progress("FX rates", $"Seeded {storedRateEvents:N0} of {rateEvents.Count:N0} FX rate events.", eventCount, false);
            });

        progress("FX rates", $"Seeded {rateEvents.Count:N0} FX rate events.", 0, true);
    }

    private static IReadOnlyList<FXCreatedEvent> CreateInitialFXCreatedEvents(IReadOnlyList<FXPairSeed> pairSeeds) =>
        pairSeeds
            .Select(pair => FXCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(SeedFXData.RateStartDate),
                AuditDateTimeBuilder.Create(SeedFXData.RateStartDate.AddMinutes(1)),
                Constants.Initialisation.Reason,
                pair.Pair.BaseCurrency,
                pair.Pair.QuoteCurrency,
                active: true).Value!)
            .ToList();

    private static IEnumerable<FXRateSetEvent> CreateInitialFXRateSetEvents(IReadOnlyList<FXPairSeed> pairSeeds) =>
        SeedFXData.CreateRateSeeds(pairSeeds)
            .Select(rate => FXRateSetEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(rate.Timestamp),
                AuditDateTimeBuilder.Create(rate.Timestamp.AddMinutes(1)),
                Constants.Initialisation.Reason,
                rate.Pair,
                rate.Price).Value!);

    private async Task CreateInstrumentSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var instrumentSeeds = SeedInstrumentData.CreateInstrumentSeeds();
        var createdEvents = CreateInitialInstrumentCreatedEvents(instrumentSeeds);
        var identifierEvents = CreateInitialInstrumentIdentifierSetEvents(instrumentSeeds);
        var termsEvents = CreateInitialInstrumentTermsSetEvents(instrumentSeeds);
        var priceEvents = CreateInitialInstrumentPriceSetEvents(instrumentSeeds).ToList();
        var incomeEvents = CreateInitialInstrumentIncomeSetEvents(instrumentSeeds);

        progress("Instruments", $"Seeding {createdEvents.Count:N0} instrument events.", 0, false);

        await StoreEvents<Instruments, InstrumentCreatedEvent>(
            Constants.Initialisation.InstrumentsStreamId,
            createdEvents,
            cancellationToken);

        progress("Instruments", $"Seeded {createdEvents.Count:N0} instrument events.", createdEvents.Count, true);
        progress("Instrument identifiers", $"Seeding {identifierEvents.Count:N0} identifier events.", 0, false);

        await AppendEvents(
            Constants.Initialisation.InstrumentsStreamId,
            identifierEvents,
            cancellationToken);

        progress("Instrument identifiers", $"Seeded {identifierEvents.Count:N0} identifier events.", identifierEvents.Count, true);
        progress("Instrument terms", $"Seeding {termsEvents.Count:N0} terms events.", 0, false);

        await AppendEvents(
            Constants.Initialisation.InstrumentsStreamId,
            termsEvents,
            cancellationToken);

        progress("Instrument terms", $"Seeded {termsEvents.Count:N0} terms events.", termsEvents.Count, true);
        progress("Instrument prices", $"Seeding {priceEvents.Count:N0} instrument price events.", 0, false);

        var storedPriceEvents = 0;
        await StoreEventsInBatches<InstrumentValues, InstrumentPriceSetEvent>(
            Constants.Initialisation.InstrumentPricesStreamId,
            priceEvents,
            cancellationToken,
            eventCount =>
            {
                storedPriceEvents += eventCount;
                progress("Instrument prices", $"Seeded {storedPriceEvents:N0} of {priceEvents.Count:N0} instrument price events.", eventCount, false);
            });

        progress("Instrument prices", $"Seeded {priceEvents.Count:N0} instrument price events.", 0, true);
        progress("Instrument income", $"Seeding {incomeEvents.Count:N0} instrument income events.", 0, false);

        await StoreEvents<InstrumentValues, InstrumentIncomeSetEvent>(
            Constants.Initialisation.InstrumentIncomesStreamId,
            incomeEvents,
            cancellationToken);

        progress("Instrument income", $"Seeded {incomeEvents.Count:N0} instrument income events.", incomeEvents.Count, true);
    }

    private static IReadOnlyList<InstrumentCreatedEvent> CreateInitialInstrumentCreatedEvents(IReadOnlyList<InstrumentSeed> instrumentSeeds) =>
        instrumentSeeds
            .Select(seed => InstrumentCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(SeedInstrumentData.ValueStartDate.AddDays(-1)),
                AuditDateTimeBuilder.Create(SeedInstrumentData.ValueStartDate.AddDays(-1).AddMinutes(1)),
                Constants.Initialisation.Reason,
                seed.InstrumentID,
                seed.Name,
                seed.FormalName,
                ExchangeBuilder.Create(seed.Exchange),
                CFIBuilder.Create(seed.Cfi),
                seed.Logo,
                active: true,
                seed.Country,
                seed.Country,
                seed.Currency).Value!)
            .ToList();

    private static IReadOnlyList<InstrumentIdentifierSetEvent> CreateInitialInstrumentIdentifierSetEvents(IReadOnlyList<InstrumentSeed> instrumentSeeds)
    {
        var eventDateTime = EventDateTimeBuilder.Create(SeedInstrumentData.ValueStartDate.AddDays(-1).AddMinutes(5));
        var auditDateTime = AuditDateTimeBuilder.Create(SeedInstrumentData.ValueStartDate.AddDays(-1).AddMinutes(6));
        var events = new List<InstrumentIdentifierSetEvent>();

        foreach (var seed in instrumentSeeds)
        {
            events.Add(InstrumentIdentifierSetEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                eventDateTime,
                auditDateTime,
                Constants.Initialisation.Reason,
                seed.InstrumentID,
                new InstrumentIdentifier(InstrumentIdentifierType.Ticker, seed.Ticker)).Value!);

            events.Add(InstrumentIdentifierSetEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                eventDateTime,
                auditDateTime,
                Constants.Initialisation.Reason,
                seed.InstrumentID,
                new InstrumentIdentifier(InstrumentIdentifierType.Sedol, seed.Sedol)).Value!);
        }

        return events;
    }

    private static IReadOnlyList<InstrumentTermsSetEvent> CreateInitialInstrumentTermsSetEvents(IReadOnlyList<InstrumentSeed> instrumentSeeds)
    {
        var eventDateTime = EventDateTimeBuilder.Create(SeedInstrumentData.ValueStartDate.AddDays(-1).AddMinutes(10));
        var auditDateTime = AuditDateTimeBuilder.Create(SeedInstrumentData.ValueStartDate.AddDays(-1).AddMinutes(11));

        return instrumentSeeds
            .Where(seed => seed.Terms is not null)
            .Select(seed => InstrumentTermsSetEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                eventDateTime,
                auditDateTime,
                Constants.Initialisation.Reason,
                seed.InstrumentID,
                seed.Terms!).Value!)
            .ToList();
    }

    private static IEnumerable<InstrumentPriceSetEvent> CreateInitialInstrumentPriceSetEvents(IReadOnlyList<InstrumentSeed> instrumentSeeds) =>
        SeedInstrumentData.CreatePriceSeeds(instrumentSeeds)
            .Select(seed => InstrumentPriceSetEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(seed.Timestamp),
                AuditDateTimeBuilder.Create(seed.Timestamp.AddMinutes(1)),
                Constants.Initialisation.Reason,
                seed.InstrumentID,
                seed.Price).Value!);

    private static IReadOnlyList<InstrumentIncomeSetEvent> CreateInitialInstrumentIncomeSetEvents(IReadOnlyList<InstrumentSeed> instrumentSeeds) =>
        SeedInstrumentData.CreateIncomeSeeds(instrumentSeeds)
            .Select(seed => InstrumentIncomeSetEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(seed.Timestamp),
                AuditDateTimeBuilder.Create(seed.Timestamp.AddMinutes(1)),
                Constants.Initialisation.Reason,
                seed.InstrumentID,
                seed.Income).Value!)
            .ToList();

    private async Task StoreEvents<TAggregate, TEvent>(Guid streamId, IEnumerable<TEvent> events, CancellationToken cancellationToken)
        where TAggregate : class, IAggregate
        where TEvent : class, IEventBase
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        var eventData = events.ToList();
        if (eventData.Count == 0)
            return;

        await eventRepository.StartStreamAsync<TAggregate, TEvent>(streamId, eventData, cancellationToken);
    }

    private async Task StoreEventsInBatches<TAggregate, TEvent>(Guid streamId, IEnumerable<TEvent> events, CancellationToken cancellationToken, Action<int>? batchStored = null)
        where TAggregate : class, IAggregate
        where TEvent : class, IEventBase
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        var firstBatch = true;
        foreach (var batch in events.Chunk(5_000))
        {
            if (firstBatch)
            {
                await eventRepository.StartStreamAsync<TAggregate, TEvent>(streamId, batch, cancellationToken);
                batchStored?.Invoke(batch.Length);
                firstBatch = false;
                continue;
            }

            await eventRepository.AppendAsync(streamId, batch, cancellationToken);
            batchStored?.Invoke(batch.Length);
        }
    }

    private async Task AppendEvents<TEvent>(Guid streamId, IEnumerable<TEvent> events, CancellationToken cancellationToken)
        where TEvent : class, IEventBase
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        foreach (var @event in events)
            await eventRepository.AppendAsync(streamId, @event, cancellationToken);
    }

    private async Task AppendEventsInBatches(Guid streamId, IEnumerable<IEventBase> events, CancellationToken cancellationToken, Action<int>? batchStored = null)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        foreach (var batch in events.Chunk(5_000))
        {
            await eventRepository.AppendAsync(streamId, batch, cancellationToken);
            batchStored?.Invoke(batch.Length);
        }
    }

    private static Action<string, string, int, bool> CreateBuildProgress(IProgress<BuildProgress>? progress, int totalEvents)
    {
        var completedSteps = 0;
        var completedEvents = 0;

        return (stage, message, eventCount, completeStep) =>
        {
            if (completeStep)
                completedSteps++;

            completedEvents += eventCount;

            progress?.Report(new BuildProgress(
                stage,
                message,
                completedSteps,
                TotalBuildSteps,
                completedEvents,
                totalEvents));
        };
    }

    private static Guid CreateDeterministicGuid(string value)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }

    private static int CountSeedEvents()
    {
        var countryEvents = CreateInitialCountryCreatedEvents().Count
            + CreateInitialCountryFlagModifiedEvents().Count
            + CreateInitialCountryModifiedEvents().Count;
        var currencyEvents = CreateInitialCurrencyCreatedEvents().Count
            + CreateInitialCurrencyModifiedEvents().Count;
        var brokerEvents = CreateInitialBrokerCreatedEvents().Count;
        var accountEvents = CreateInitialAccountCreatedEvents().Count
            + CreateInitialAccountModifiedEvents().Count
            + CreateInitialAccountActiveModifiedEvents().Count;
        var valuationSettingEvents = CreateInitialAssetAllocationCreatedEvents().Count;
        var reportEvents = CreateInitialReportCreatedEvents().Count;
        var pairSeeds = SeedFXData.CreatePairSeeds();
        var fxEvents = CreateInitialFXCreatedEvents(pairSeeds).Count
            + CreateInitialFXRateSetEvents(pairSeeds).Count();
        var instrumentSeeds = SeedInstrumentData.CreateInstrumentSeeds();
        var instrumentEvents = CreateInitialInstrumentCreatedEvents(instrumentSeeds).Count
            + CreateInitialInstrumentIdentifierSetEvents(instrumentSeeds).Count
            + CreateInitialInstrumentTermsSetEvents(instrumentSeeds).Count
            + CreateInitialInstrumentPriceSetEvents(instrumentSeeds).Count()
            + CreateInitialInstrumentIncomeSetEvents(instrumentSeeds).Count;
        var holdingCreatedEvents = CreateInitialHoldingCreatedEvents(instrumentSeeds);
        var holdingEvents = holdingCreatedEvents.Count;
        var transactionEvents = CreateInitialTransactionEvents(instrumentSeeds, holdingCreatedEvents).Count;

        return countryEvents + currencyEvents + brokerEvents + accountEvents + valuationSettingEvents + reportEvents + fxEvents + instrumentEvents + holdingEvents + transactionEvents;
    }

    private sealed class SeedPositionState
    {
        public decimal Quantity { get; set; }

        public decimal BookCost { get; set; }
    }
}
