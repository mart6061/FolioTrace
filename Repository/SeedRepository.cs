using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository.Seed;

namespace Repository;

public sealed class SeedRepository(IEventRepository eventRepository, IFXRateReadModelRepository fxRateReadModelRepository, IInstrumentValueReadModelRepository instrumentValueReadModelRepository) : ISeedRepository
{
    private const int TotalBuildSteps = 10;

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
        await instrumentValueReadModelRepository.ClearAsync(cancellationToken);
        progress("Clear", "Events and projections cleared.", 0, true);
    }

    private async Task CreateSetupEvents(Action<string, string, int, bool> progress, CancellationToken cancellationToken)
    {
        await CreateCountrySetupEvents(progress, cancellationToken);
        await CreateCurrencySetupEvents(progress, cancellationToken);
        await CreateFXSetupEvents(progress, cancellationToken);
        await CreateInstrumentSetupEvents(progress, cancellationToken);
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
                Guid.NewGuid(),
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
                Guid.NewGuid(),
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
                Guid.NewGuid(),
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
                Guid.NewGuid(),
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
                Guid.NewGuid(),
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
                Guid.NewGuid(),
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
                Guid.NewGuid(),
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
                Guid.NewGuid(),
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
                seed.Country).Value!)
            .ToList();

    private static IReadOnlyList<InstrumentIdentifierSetEvent> CreateInitialInstrumentIdentifierSetEvents(IReadOnlyList<InstrumentSeed> instrumentSeeds)
    {
        var eventDateTime = EventDateTimeBuilder.Create(SeedInstrumentData.ValueStartDate.AddDays(-1).AddMinutes(5));
        var auditDateTime = AuditDateTimeBuilder.Create(SeedInstrumentData.ValueStartDate.AddDays(-1).AddMinutes(6));
        var events = new List<InstrumentIdentifierSetEvent>();

        foreach (var seed in instrumentSeeds)
        {
            events.Add(InstrumentIdentifierSetEventBuilder.CreateSeed(
                Guid.NewGuid(),
                Constants.Initialisation.UserID,
                eventDateTime,
                auditDateTime,
                Constants.Initialisation.Reason,
                seed.InstrumentID,
                new InstrumentIdentifier(InstrumentIdentifierType.Ticker, seed.Ticker)).Value!);

            events.Add(InstrumentIdentifierSetEventBuilder.CreateSeed(
                Guid.NewGuid(),
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
                Guid.NewGuid(),
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
                Guid.NewGuid(),
                Constants.Initialisation.UserID,
                EventDateTimeBuilder.Create(seed.Timestamp),
                AuditDateTimeBuilder.Create(seed.Timestamp.AddMinutes(1)),
                Constants.Initialisation.Reason,
                seed.InstrumentID,
                seed.Price).Value!);

    private static IReadOnlyList<InstrumentIncomeSetEvent> CreateInitialInstrumentIncomeSetEvents(IReadOnlyList<InstrumentSeed> instrumentSeeds) =>
        SeedInstrumentData.CreateIncomeSeeds(instrumentSeeds)
            .Select(seed => InstrumentIncomeSetEventBuilder.CreateSeed(
                Guid.NewGuid(),
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

    private static int CountSeedEvents()
    {
        var countryEvents = CreateInitialCountryCreatedEvents().Count
            + CreateInitialCountryFlagModifiedEvents().Count
            + CreateInitialCountryModifiedEvents().Count;
        var currencyEvents = CreateInitialCurrencyCreatedEvents().Count
            + CreateInitialCurrencyModifiedEvents().Count;
        var pairSeeds = SeedFXData.CreatePairSeeds();
        var fxEvents = CreateInitialFXCreatedEvents(pairSeeds).Count
            + CreateInitialFXRateSetEvents(pairSeeds).Count();
        var instrumentSeeds = SeedInstrumentData.CreateInstrumentSeeds();
        var instrumentEvents = CreateInitialInstrumentCreatedEvents(instrumentSeeds).Count
            + CreateInitialInstrumentIdentifierSetEvents(instrumentSeeds).Count
            + CreateInitialInstrumentTermsSetEvents(instrumentSeeds).Count
            + CreateInitialInstrumentPriceSetEvents(instrumentSeeds).Count()
            + CreateInitialInstrumentIncomeSetEvents(instrumentSeeds).Count;

        return countryEvents + currencyEvents + fxEvents + instrumentEvents;
    }
}
