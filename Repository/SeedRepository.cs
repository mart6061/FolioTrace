using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository.Seed;

namespace Repository;

public sealed class SeedRepository(IEventRepository eventRepository, IFXRateReadModelRepository fxRateReadModelRepository) : ISeedRepository
{
    private const int TotalBuildSteps = 17;
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
        await CreateInputControlSettingsSetupEvents(progress, cancellationToken);
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

    private async Task CreateInputControlSettingsSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        var createdEvents = CreateInitialInputControlSettingsCreatedEvents();
        var eventCount = createdEvents.Count;

        progress("Input policies", $"Seeding {eventCount:N0} input control setting events.", 0, false);

        await StoreEvents<InputControlSettings, InputControlSettingsCreatedEvent>(
            Constants.Initialisation.InputControlSettingsStreamId,
            createdEvents,
            cancellationToken);

        progress("Input policies", $"Seeded {eventCount:N0} input control setting events.", eventCount, true);
    }

    public static IReadOnlyList<InputControlSettingsCreatedEvent> CreateInitialInputControlSettingsCreatedEvents()
    {
        var eventDateTime = EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(44));
        var auditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(44));
        var usAccountID = SeedAccounts.Single(account => account.Name == "US Broker").AccountID;

        var settings = new List<InputControlSettingDefinition>
        {
            new(InputControlKind.Quantity, InputControlSettingScope.Global, null, null, 4, 0.0001m, null, "#,##0.####", false),
            new(InputControlKind.Money, InputControlSettingScope.Global, null, null, null, 0m, null, "#,##0.00##", false),
            new(InputControlKind.Quantity, InputControlSettingScope.User, null, Constants.Initialisation.UserID, 6, null, null, "#,##0.######", null),
            new(InputControlKind.Quantity, InputControlSettingScope.Account, usAccountID, null, 2, 1m, null, "#,##0.##", false)
        };

        return
        [
            InputControlSettingsCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                eventDateTime,
                auditDateTime,
                Constants.Initialisation.Reason,
                settings).Value!
        ];
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
        var accountIDs = SeedAccounts.Select(account => account.AccountID).ToList();
        var detailedEventDateTime = EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(45));
        var detailedAuditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(45));
        var summaryEventDateTime = EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(46));
        var summaryAuditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(46));
        var assetsListEventDateTime = EventDateTimeBuilder.Create(Constants.Initialisation.EventDateTime.Value.AddTicks(47));
        var assetsListAuditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(47));

        return
        [
            AssetAllocationCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                detailedEventDateTime,
                detailedEventDateTime,
                detailedAuditDateTime,
                Constants.Initialisation.Reason,
                AssetAllocationIDBuilder.Create(CreateDeterministicGuid("asset-allocation-detailed")),
                "Detailed",
                accountIDs,
                true,
                NodeIDBuilder.Create(CreateDeterministicGuid("asset-allocation-detailed-root")),
                CreateDetailedAssetAllocationNodes()).Value!,
            AssetAllocationCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                summaryEventDateTime,
                summaryEventDateTime,
                summaryAuditDateTime,
                Constants.Initialisation.Reason,
                AssetAllocationIDBuilder.Create(CreateDeterministicGuid("asset-allocation-summary")),
                "Summary",
                accountIDs,
                true,
                NodeIDBuilder.Create(CreateDeterministicGuid("asset-allocation-summary-root")),
                CreateSummaryAssetAllocationNodes()).Value!,
            AssetAllocationCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                assetsListEventDateTime,
                assetsListEventDateTime,
                assetsListAuditDateTime,
                Constants.Initialisation.Reason,
                AssetAllocationIDBuilder.Create(CreateDeterministicGuid("asset-allocation-assets-list")),
                "Assets List",
                accountIDs,
                true,
                NodeIDBuilder.Create(CreateDeterministicGuid("asset-allocation-assets-list-root")),
                CreateAssetsListAssetAllocationNodes()).Value!
        ];
    }

    private static List<AssetAllocationNode> CreateDetailedAssetAllocationNodes()
    {
        var nodeIDs = CreateNodeIDs(
            "Detailed/Equities",
            "Detailed/Equities/UK",
            "Detailed/Equities/Europe",
            "Detailed/Equities/US",
            "Detailed/Equities/Other",
            "Detailed/Fixed Interest",
            "Detailed/Fixed Interest/Government",
            "Detailed/Fixed Interest/Corp",
            "Detailed/Alternative",
            "Detailed/Memo",
            "Detailed/Cash",
            "Detailed/Equities/UK/Basic Materials",
            "Detailed/Equities/UK/Basic Materials/Mining",
            "Detailed/Equities/UK/Basic Materials/Chemicals",
            "Detailed/Equities/UK/Basic Materials/Forestry & Paper",
            "Detailed/Equities/UK/Consumer Discretionary",
            "Detailed/Equities/UK/Consumer Discretionary/Automotive",
            "Detailed/Equities/UK/Consumer Discretionary/Household Goods",
            "Detailed/Equities/UK/Consumer Discretionary/Leisure Goods",
            "Detailed/Equities/UK/Consumer Discretionary/Media",
            "Detailed/Equities/UK/Consumer Discretionary/Travel & Leisure",
            "Detailed/Equities/UK/Consumer Discretionary/Retailers",
            "Detailed/Equities/UK/Consumer Staples",
            "Detailed/Equities/UK/Consumer Staples/Food Producers",
            "Detailed/Equities/UK/Consumer Staples/Beverages",
            "Detailed/Equities/UK/Consumer Staples/Tobacco",
            "Detailed/Equities/UK/Consumer Staples/Personal Care, Drug & Grocery Stores",
            "Detailed/Equities/UK/Energy",
            "Detailed/Equities/UK/Energy/Oil, Gas & Coal",
            "Detailed/Equities/UK/Energy/Energy Equipment & Services",
            "Detailed/Equities/UK/Financials",
            "Detailed/Equities/UK/Financials/Banks",
            "Detailed/Equities/UK/Financials/Insurance",
            "Detailed/Equities/UK/Financials/Financial Services",
            "Detailed/Equities/UK/Financials/Investment Banking & Brokerage",
            "Detailed/Equities/UK/Health Care",
            "Detailed/Equities/UK/Health Care/Medical Equipment & Services",
            "Detailed/Equities/UK/Health Care/Pharmaceuticals & Biotechnology",
            "Detailed/Equities/UK/Industrials",
            "Detailed/Equities/UK/Industrials/Construction & Materials",
            "Detailed/Equities/UK/Industrials/Aerospace & Defense",
            "Detailed/Equities/UK/Industrials/Electronic & Electrical Equipment",
            "Detailed/Equities/UK/Industrials/Industrial Engineering",
            "Detailed/Equities/UK/Industrials/Industrial Transportation",
            "Detailed/Equities/UK/Industrials/Support Services",
            "Detailed/Equities/UK/Real Estate",
            "Detailed/Equities/UK/Real Estate/Real Estate Holding & Development",
            "Detailed/Equities/UK/Real Estate/Real Estate Investment Trusts (REITs)",
            "Detailed/Equities/UK/Technology",
            "Detailed/Equities/UK/Technology/Software & Computer Services",
            "Detailed/Equities/UK/Technology/Technology Hardware & Equipment",
            "Detailed/Equities/UK/Telecommunications",
            "Detailed/Equities/UK/Telecommunications/Telecommunication Service Providers");

        return
        [
            CreateSeedNode(nodeIDs, "Detailed/Equities", "Equities", ["Detailed/Equities/UK", "Detailed/Equities/Europe", "Detailed/Equities/US", "Detailed/Equities/Other"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK", "UK", ["Detailed/Equities/UK/Basic Materials", "Detailed/Equities/UK/Consumer Discretionary", "Detailed/Equities/UK/Consumer Staples", "Detailed/Equities/UK/Energy", "Detailed/Equities/UK/Financials", "Detailed/Equities/UK/Health Care", "Detailed/Equities/UK/Industrials", "Detailed/Equities/UK/Real Estate", "Detailed/Equities/UK/Technology", "Detailed/Equities/UK/Telecommunications"], "#1d4ed8"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/Europe", "Europe", [], "#2563eb"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/US", "US", [], "#3b82f6"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/Other", "Other", [], "#60a5fa"),
            CreateSeedNode(nodeIDs, "Detailed/Fixed Interest", "Fixed Interest", ["Detailed/Fixed Interest/Government", "Detailed/Fixed Interest/Corp"]),
            CreateSeedNode(nodeIDs, "Detailed/Fixed Interest/Government", "Government", [], "#047857"),
            CreateSeedNode(nodeIDs, "Detailed/Fixed Interest/Corp", "Corp", [], "#34d399"),
            CreateSeedNode(nodeIDs, "Detailed/Alternative", "Alternative", [], "#7c3aed"),
            CreateSeedNode(nodeIDs, "Detailed/Memo", "Memo", [], "#64748b"),
            CreateSeedNode(nodeIDs, "Detailed/Cash", "Cash", [], "#059669"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Basic Materials", "Basic Materials", ["Detailed/Equities/UK/Basic Materials/Mining", "Detailed/Equities/UK/Basic Materials/Chemicals", "Detailed/Equities/UK/Basic Materials/Forestry & Paper"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Basic Materials/Mining", "Mining"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Basic Materials/Chemicals", "Chemicals"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Basic Materials/Forestry & Paper", "Forestry & Paper"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Discretionary", "Consumer Discretionary", ["Detailed/Equities/UK/Consumer Discretionary/Automotive", "Detailed/Equities/UK/Consumer Discretionary/Household Goods", "Detailed/Equities/UK/Consumer Discretionary/Leisure Goods", "Detailed/Equities/UK/Consumer Discretionary/Media", "Detailed/Equities/UK/Consumer Discretionary/Travel & Leisure", "Detailed/Equities/UK/Consumer Discretionary/Retailers"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Discretionary/Automotive", "Automotive"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Discretionary/Household Goods", "Household Goods"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Discretionary/Leisure Goods", "Leisure Goods"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Discretionary/Media", "Media"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Discretionary/Travel & Leisure", "Travel & Leisure"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Discretionary/Retailers", "Retailers"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Staples", "Consumer Staples", ["Detailed/Equities/UK/Consumer Staples/Food Producers", "Detailed/Equities/UK/Consumer Staples/Beverages", "Detailed/Equities/UK/Consumer Staples/Tobacco", "Detailed/Equities/UK/Consumer Staples/Personal Care, Drug & Grocery Stores"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Staples/Food Producers", "Food Producers"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Staples/Beverages", "Beverages"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Staples/Tobacco", "Tobacco"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Consumer Staples/Personal Care, Drug & Grocery Stores", "Personal Care, Drug & Grocery Stores"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Energy", "Energy", ["Detailed/Equities/UK/Energy/Oil, Gas & Coal", "Detailed/Equities/UK/Energy/Energy Equipment & Services"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Energy/Oil, Gas & Coal", "Oil, Gas & Coal"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Energy/Energy Equipment & Services", "Energy Equipment & Services"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Financials", "Financials", ["Detailed/Equities/UK/Financials/Banks", "Detailed/Equities/UK/Financials/Insurance", "Detailed/Equities/UK/Financials/Financial Services", "Detailed/Equities/UK/Financials/Investment Banking & Brokerage"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Financials/Banks", "Banks"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Financials/Insurance", "Insurance"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Financials/Financial Services", "Financial Services"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Financials/Investment Banking & Brokerage", "Investment Banking & Brokerage"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Health Care", "Health Care", ["Detailed/Equities/UK/Health Care/Medical Equipment & Services", "Detailed/Equities/UK/Health Care/Pharmaceuticals & Biotechnology"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Health Care/Medical Equipment & Services", "Medical Equipment & Services"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Health Care/Pharmaceuticals & Biotechnology", "Pharmaceuticals & Biotechnology"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Industrials", "Industrials", ["Detailed/Equities/UK/Industrials/Construction & Materials", "Detailed/Equities/UK/Industrials/Aerospace & Defense", "Detailed/Equities/UK/Industrials/Electronic & Electrical Equipment", "Detailed/Equities/UK/Industrials/Industrial Engineering", "Detailed/Equities/UK/Industrials/Industrial Transportation", "Detailed/Equities/UK/Industrials/Support Services"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Industrials/Construction & Materials", "Construction & Materials"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Industrials/Aerospace & Defense", "Aerospace & Defense"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Industrials/Electronic & Electrical Equipment", "Electronic & Electrical Equipment"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Industrials/Industrial Engineering", "Industrial Engineering"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Industrials/Industrial Transportation", "Industrial Transportation"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Industrials/Support Services", "Support Services"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Real Estate", "Real Estate", ["Detailed/Equities/UK/Real Estate/Real Estate Holding & Development", "Detailed/Equities/UK/Real Estate/Real Estate Investment Trusts (REITs)"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Real Estate/Real Estate Holding & Development", "Real Estate Holding & Development"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Real Estate/Real Estate Investment Trusts (REITs)", "Real Estate Investment Trusts (REITs)"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Technology", "Technology", ["Detailed/Equities/UK/Technology/Software & Computer Services", "Detailed/Equities/UK/Technology/Technology Hardware & Equipment"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Technology/Software & Computer Services", "Software & Computer Services"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Technology/Technology Hardware & Equipment", "Technology Hardware & Equipment"),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Telecommunications", "Telecommunications", ["Detailed/Equities/UK/Telecommunications/Telecommunication Service Providers"]),
            CreateSeedNode(nodeIDs, "Detailed/Equities/UK/Telecommunications/Telecommunication Service Providers", "Telecommunication Service Providers")
        ];
    }

    private static List<AssetAllocationNode> CreateSummaryAssetAllocationNodes()
    {
        var nodeIDs = CreateNodeIDs(
            "Summary/Equities",
            "Summary/Fixed Interest",
            "Summary/Alternative",
            "Summary/Memo",
            "Summary/Cash");

        return
        [
            CreateSeedNode(nodeIDs, "Summary/Equities", "Equities", [], "#2563eb"),
            CreateSeedNode(nodeIDs, "Summary/Fixed Interest", "Fixed Interest", [], "#059669"),
            CreateSeedNode(nodeIDs, "Summary/Alternative", "Alternative", [], "#7c3aed"),
            CreateSeedNode(nodeIDs, "Summary/Memo", "Memo", [], "#64748b"),
            CreateSeedNode(nodeIDs, "Summary/Cash", "Cash", [], "#059669")
        ];
    }

    private static List<AssetAllocationNode> CreateAssetsListAssetAllocationNodes()
    {
        var nodeIDs = CreateNodeIDs(
            "Assets List/Assets",
            "Assets List/Cash");

        return
        [
            CreateSeedNode(nodeIDs, "Assets List/Assets", "Assets", [], "#000000"),
            CreateSeedNode(nodeIDs, "Assets List/Cash", "Cash", [], "#6b7280")
        ];
    }

    private static Dictionary<string, NodeID> CreateNodeIDs(params string[] paths) =>
        paths.ToDictionary(path => path, path => NodeIDBuilder.Create(CreateDeterministicGuid($"asset-allocation-node-{path}")));

    private static AssetAllocationNode CreateSeedNode(Dictionary<string, NodeID> nodeIDs, string path, string name, IReadOnlyList<string>? childPaths = null, string? colour = null) =>
        new(
            nodeIDs[path],
            childPaths?.Select(childPath => nodeIDs[childPath]).ToList() ?? [],
            name,
            false,
            false,
            [],
            colour);

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
        var detailedAssetAllocationID = valuationSettings.Items.Single(setting => setting.Name == "Detailed").AssetAllocationID;
        var summaryAssetAllocationID = valuationSettings.Items.Single(setting => setting.Name == "Summary").AssetAllocationID;
        var reportID = ReportIDBuilder.Create(CreateDeterministicGuid("report-current"));
        var imReportID = ReportIDBuilder.Create(CreateDeterministicGuid("report-im"));
        var eventDateTime = EventDateTimeBuilder.Create(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var auditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(46));
        var imAuditDateTime = AuditDateTimeBuilder.Create(Constants.Initialisation.AuditDateTime.Value.AddTicks(47));

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
                CreateCurrentReportNodes(detailedAssetAllocationID),
                valuationSettings: valuationSettings).Value!,
            ReportCreatedEventBuilder.CreateSeed(
                Guid.CreateGuid7(),
                Constants.Initialisation.UserID,
                eventDateTime,
                imAuditDateTime,
                Constants.Initialisation.Reason,
                imReportID,
                "IM",
                true,
                eventDateTime,
                CreateIMReportNodes(summaryAssetAllocationID, detailedAssetAllocationID),
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

    private static List<ReportNodeBase> CreateIMReportNodes(AssetAllocationID summaryAssetAllocationID, AssetAllocationID detailedAssetAllocationID) =>
    [
        new ReportNodeValuation(
            ReportNodeIDBuilder.Create(CreateDeterministicGuid("report-im-summary-valuation")),
            1,
            "Summary Valuation",
            "Summary Valuation",
            summaryAssetAllocationID,
            ReportConfigBuilder.DefaultValuationColumns(),
            ColourBullet: true,
            ColourText: false,
            DisplayHoldings: false) { PageOrientation = ReportNodePageOrientation.Landscape },
        new ReportNodeValuation(
            ReportNodeIDBuilder.Create(CreateDeterministicGuid("report-im-detailed-valuation")),
            2,
            "Detailed Valuation",
            "Detailed Valuation",
            detailedAssetAllocationID,
            ReportConfigBuilder.DefaultValuationColumns(),
            ColourBullet: true,
            ColourText: false,
            DisplayHoldings: true) { PageOrientation = ReportNodePageOrientation.Landscape }
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
                events.Add(CreateSeedHolding(index++, account.AccountID, investableCashInstrument.InstrumentID, typeof(HoldingNominalInflow), $"Inflow {investableCashInstrument.Currency}", true, false));
                events.Add(CreateSeedHolding(index++, account.AccountID, investableCashInstrument.InstrumentID, typeof(HoldingNominalOutflow), $"Outflow {investableCashInstrument.Currency}", true, false));
            }

            AddNonValuationSeedHoldings(events, ref index, account, cashInstrument.InstrumentID);

            foreach (var equity in SelectSeedEquitiesForAccount(instrumentSeeds, accountIndex))
            {
                events.Add(CreateSeedHolding(index++, account.AccountID, equity.InstrumentID, typeof(HoldingPositionAsset), $"Asset {equity.Ticker}", true, false));
                events.Add(CreateSeedHolding(index++, account.AccountID, equity.InstrumentID, typeof(HoldingNominalInSpecieIn), $"InSpecie In {equity.Ticker}", true, false));
                events.Add(CreateSeedHolding(index++, account.AccountID, equity.InstrumentID, typeof(HoldingNominalInSpecieOut), $"InSpecie Out {equity.Ticker}", true, false));
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
        var fxPairSeeds = SeedFXData.CreatePairSeeds();
        var startDate = DateTime.UtcNow.Date.AddYears(-1);
        var endDate = startDate.AddYears(1);

        foreach (var account in SeedAccounts)
        {
            var accountIndex = Array.FindIndex(SeedAccounts, seed => seed.AccountID == account.AccountID);
            var random = CreateSeedRandom(account.AccountID);
            var cashInstrument = instrumentSeeds.Single(seed => seed.Kind is InstrumentSeedKind.Cash && seed.Currency == account.BookCurrency);
            var cashHolding = FindSeedHolding(holdings, account.AccountID, cashInstrument.InstrumentID, typeof(HoldingCashInvestable), $"Investable {account.BookCurrency}");
            var inflowHolding = FindSeedHolding(holdings, account.AccountID, cashInstrument.InstrumentID, typeof(HoldingNominalInflow), $"Inflow {account.BookCurrency}");
            var outflowHolding = FindSeedHolding(holdings, account.AccountID, cashInstrument.InstrumentID, typeof(HoldingNominalOutflow), $"Outflow {account.BookCurrency}");
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
                    Alpha3Builder.Create(cashInstrument.Currency),
                    Alpha3Builder.Create(account.BookCurrency),
                    fxPairSeeds,
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
                    Alpha3Builder.Create(cashInstrument.Currency),
                    Alpha3Builder.Create(account.BookCurrency),
                    fxPairSeeds,
                    monthDate.AddDays(2).AddHours(9),
                    $"Seed monthly cash out {month + 1}"));

                for (var weekDate = startDate; weekDate < endDate; weekDate = weekDate.AddDays(7))
                {
                    if (weekDate < monthDate || weekDate >= monthDate.AddMonths(1))
                        continue;

                    foreach (var equity in selectedEquities.Select((seed, index) => (Seed: seed, Index: index)))
                    {
                        var localCost = RoundMoney(inflowAmount * 0.10m);
                        var localCostCurrency = Alpha3Builder.Create(equity.Seed.Currency);
                        var bookCurrency = Alpha3Builder.Create(account.BookCurrency);
                        var resolvedBookCost = ResolveSeedBookCost(localCost, localCostCurrency, bookCurrency, fxPairSeeds, $"Seed weekly InSpecie in {equity.Seed.Ticker}");
                        var quantity = RoundQuantity(localCost / equity.Seed.BasePrice);
                        if (quantity <= 0m || localCost <= 0m)
                            continue;

                        var assetHolding = FindSeedHolding(holdings, account.AccountID, equity.Seed.InstrumentID, typeof(HoldingPositionAsset), $"Asset {equity.Seed.Ticker}");
                        var inSpecieInHolding = FindSeedHolding(holdings, account.AccountID, equity.Seed.InstrumentID, typeof(HoldingNominalInSpecieIn), $"InSpecie In {equity.Seed.Ticker}");
                        var positionState = positionStates[equity.Seed.InstrumentID];

                        events.AddRange(CreateSeedTransactionSet(
                            holdings,
                            assetHolding,
                            inSpecieInHolding,
                            equity.Seed.InstrumentID,
                            quantity,
                            localCost,
                            localCostCurrency,
                            bookCurrency,
                            fxPairSeeds,
                            weekDate.AddHours(10 + equity.Index),
                            $"Seed weekly InSpecie in {equity.Seed.Ticker}"));

                        positionState.Quantity += quantity;
                        positionState.BookCost += resolvedBookCost.BookCost.Value;
                    }
                }

                var outEquity = selectedEquities[(accountIndex + month) % selectedEquities.Count];
                var state = positionStates[outEquity.InstrumentID];
                var outQuantity = RoundQuantity(state.Quantity * 0.25m);
                var outBookCost = RoundMoney(state.BookCost * 0.25m);
                if (outQuantity <= 0m || outBookCost <= 0m)
                    continue;

                var outAssetHolding = FindSeedHolding(holdings, account.AccountID, outEquity.InstrumentID, typeof(HoldingPositionAsset), $"Asset {outEquity.Ticker}");
                var inSpecieOutHolding = FindSeedHolding(holdings, account.AccountID, outEquity.InstrumentID, typeof(HoldingNominalInSpecieOut), $"InSpecie Out {outEquity.Ticker}");
                var outLocalCostCurrency = Alpha3Builder.Create(outEquity.Currency);
                var outBookCurrency = Alpha3Builder.Create(account.BookCurrency);
                var outLocalCost = ResolveSeedLocalCost(outBookCost, outLocalCostCurrency, outBookCurrency, fxPairSeeds, $"Seed monthly InSpecie out {outEquity.Ticker}");

                events.AddRange(CreateSeedTransactionSet(
                    holdings,
                    inSpecieOutHolding,
                    outAssetHolding,
                    outEquity.InstrumentID,
                    outQuantity,
                    outLocalCost,
                    outLocalCostCurrency,
                    outBookCurrency,
                    fxPairSeeds,
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

    private static IReadOnlyList<ITransactionEvent> CreateSeedTransactionSet(
        Holdings holdings,
        HoldingBase creditHolding,
        HoldingBase debitHolding,
        InstrumentID instrumentID,
        decimal quantity,
        decimal localCost,
        Alpha3 localCostCurrency,
        Alpha3 bookCurrency,
        IReadOnlyList<FXPairSeed> fxPairSeeds,
        DateTime eventDateTime,
        string reason)
    {
        var bookCost = ResolveSeedBookCost(localCost, localCostCurrency, bookCurrency, fxPairSeeds, reason);
        var request = new TransactionSetRequest(
            Constants.Initialisation.UserID,
            EventDateTimeBuilder.Create(eventDateTime),
            SettlementDateTimeBuilder.Create(eventDateTime.AddDays(2)),
            reason,
            [CreateSeedTransactionLeg(creditHolding, instrumentID, quantity, localCost, localCostCurrency, bookCost)],
            [CreateSeedTransactionLeg(debitHolding, instrumentID, quantity, localCost, localCostCurrency, bookCost)]);
        var result = TransactionBuilder.Create(request, holdings);

        if (result.IsValid && result.Value is not null)
            return result.Value.Cast<ITransactionEvent>().ToList();

        throw new InvalidOperationException($"Unable to create seed transaction '{reason}': {string.Join("; ", result.ValidationErrors)}");
    }

    private static ResolvedSeedBookCost ResolveSeedBookCost(decimal localCost, Alpha3 localCostCurrency, Alpha3 bookCurrency, IReadOnlyList<FXPairSeed> fxPairSeeds, string reason)
    {
        if (localCostCurrency == bookCurrency)
            return new ResolvedSeedBookCost(new TransactionBookCost(localCost), BookCostSource.SameCurrency, false);

        var pair = fxPairSeeds.FirstOrDefault(seed =>
            seed.Pair.BaseCurrency == localCostCurrency &&
            seed.Pair.QuoteCurrency == bookCurrency);

        if (pair is null)
            throw new InvalidOperationException($"Unable to create seed transaction '{reason}': missing seed FX {localCostCurrency}/{bookCurrency}.");

        return new ResolvedSeedBookCost(
            new TransactionBookCost(RoundMoney(localCost * pair.BaselineMid)),
            BookCostSource.FxEstimate,
            true);
    }

    private static decimal ResolveSeedLocalCost(decimal bookCost, Alpha3 localCostCurrency, Alpha3 bookCurrency, IReadOnlyList<FXPairSeed> fxPairSeeds, string reason)
    {
        if (localCostCurrency == bookCurrency)
            return bookCost;

        var pair = fxPairSeeds.FirstOrDefault(seed =>
            seed.Pair.BaseCurrency == localCostCurrency &&
            seed.Pair.QuoteCurrency == bookCurrency);

        if (pair is null)
            throw new InvalidOperationException($"Unable to create seed transaction '{reason}': missing seed FX {localCostCurrency}/{bookCurrency}.");

        return RoundMoney(bookCost / pair.BaselineMid);
    }

    private static TransactionRequest CreateSeedTransactionLeg(HoldingBase holding, InstrumentID instrumentID, decimal quantity, decimal localCost, Alpha3 localCostCurrency, ResolvedSeedBookCost bookCost) =>
        new(
            holding.HoldingID,
            instrumentID,
            holding.AccountID,
            new TransactionQuantity(quantity),
            new TransactionLocalCost(localCost),
            localCostCurrency,
            bookCost.BookCost,
            bookCost.Source,
            bookCost.Estimated);

    private sealed record ResolvedSeedBookCost(TransactionBookCost BookCost, BookCostSource Source, bool Estimated);

    private static HoldingBase FindSeedHolding(Holdings holdings, AccountID accountID, InstrumentID instrumentID, Type holdingType, string name)
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
            events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrumentID, typeof(HoldingNominalFeesCustodian), name, true, false));

        foreach (var name in administratorNames.Take(accountIndex % administratorNames.Length))
            events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrumentID, typeof(HoldingNominalFeesAdministrator), name, true, false));

        events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrumentID, typeof(HoldingNominalFeesBank), bankNames[accountIndex % bankNames.Length], true, false));
        events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrumentID, typeof(HoldingNominalIncome), bankNames[(accountIndex + 1) % bankNames.Length], true, false));
        events.Add(CreateSeedHolding(index++, account.AccountID, cashInstrumentID, typeof(HoldingNominalInterest), bankNames[accountIndex % bankNames.Length], true, false));
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
            nameof(HoldingNominalInflow) => HoldingNominalInflowCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingNominalOutflow) => HoldingNominalOutflowCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingNominalInSpecieIn) => HoldingNominalInSpecieInCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingNominalInSpecieOut) => HoldingNominalInSpecieOutCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingNominalFeesCustodian) => HoldingNominalFeesCustodianCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingNominalFeesAdministrator) => HoldingNominalFeesAdministratorCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingNominalFeesBank) => HoldingNominalFeesBankCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingNominalIncome) => HoldingNominalIncomeCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
            nameof(HoldingNominalInterest) => HoldingNominalInterestCreatedEventBuilder.CreateSeed(eventId, Constants.Initialisation.UserID, eventDateTime, auditDateTime, Constants.Initialisation.Reason, holdingID, accountID, instrumentID, name, active, isDefault).Value!,
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
        where TEvent : class, IAuditEventBase
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
        where TEvent : class, IAuditEventBase
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
        where TEvent : class, IAuditEventBase
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        foreach (var @event in events)
            await eventRepository.AppendAsync(streamId, @event, cancellationToken);
    }

    private async Task AppendEventsInBatches(Guid streamId, IEnumerable<IAuditEventBase> events, CancellationToken cancellationToken, Action<int>? batchStored = null)
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
        var inputControlSettingEvents = CreateInitialInputControlSettingsCreatedEvents().Count;
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

        return countryEvents + currencyEvents + brokerEvents + accountEvents + inputControlSettingEvents + valuationSettingEvents + reportEvents + fxEvents + instrumentEvents + holdingEvents + transactionEvents;
    }

    private sealed class SeedPositionState
    {
        public decimal Quantity { get; set; }

        public decimal BookCost { get; set; }
    }
}
