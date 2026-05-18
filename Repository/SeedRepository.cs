using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository.Seed;

namespace Repository;

public sealed class SeedRepository(IEventRepository eventRepository) : ISeedRepository
{
    public async Task Build(CancellationToken cancellationToken = default)
    {
        await DeleteEvents(cancellationToken);
        await CreateSetupEvents(cancellationToken);
    }

    private async Task DeleteEvents(CancellationToken cancellationToken)
    {
        await eventRepository.ClearAsync(cancellationToken);
    }

    private async Task CreateSetupEvents(CancellationToken cancellationToken)
    {
        await CreateCountrySetupEvents(cancellationToken);
        await CreateCurrencySetupEvents(cancellationToken);
    }

    private async Task CreateCountrySetupEvents(CancellationToken cancellationToken)
    {
        await StoreEvents<Countries, CountryCreatedEvent>(
            Constants.Initialisation.CountriesStreamId,
            CreateInitialCountryCreatedEvents(),
            cancellationToken);

        await AppendEvents(
            Constants.Initialisation.CountriesStreamId,
            CreateInitialCountryFlagModifiedEvents(),
            cancellationToken);
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

    private async Task CreateCurrencySetupEvents(CancellationToken cancellationToken)
    {
        await StoreEvents<Currencies, CurrencyCreatedEvent>(
            Constants.Initialisation.CurrenciesStreamId,
            CreateInitialCurrencyCreatedEvents(),
            cancellationToken);
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

    private async Task AppendEvents<TEvent>(Guid streamId, IEnumerable<TEvent> events, CancellationToken cancellationToken)
        where TEvent : class, IEventBase
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        foreach (var @event in events)
            await eventRepository.AppendAsync(streamId, @event, cancellationToken);
    }
}









