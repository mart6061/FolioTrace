using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Marten;

namespace Repository;

public sealed class MartenFXRateReadModelRepository(IDocumentStore store, IEventRepository eventRepository) : IFXRateReadModelRepository
{
    private const int StoreBatchSize = 5_000;

    public async Task<FXRates?> LoadAsync(EventDateTime valuationDateTime, CancellationToken cancellationToken = default)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        await using var session = store.QuerySession();

        var valuation = ToDatabaseDateTime(valuationDateTime.Value);
        var definitions = await session.Query<FXDefinitionReadModel>()
            .Where(model => model.ValidFrom <= valuation && model.ValidTo > valuation)
            .ToListAsync(cancellationToken);

        if (definitions.Count == 0)
            return null;

        var pairs = definitions
            .Select(model => model.Pair)
            .ToArray();

        var ratePoints = await session.Query<FXRatePointReadModel>()
            .Where(model => pairs.Contains(model.Pair) && model.ValidFrom <= valuation && model.ValidTo > valuation)
            .ToListAsync(cancellationToken);

        var definitionsByPair = definitions.ToDictionary(model => model.Pair, StringComparer.Ordinal);
        var items = new List<FXRate>();
        foreach (var ratePoint in ratePoints.OrderBy(model => model.Pair, StringComparer.Ordinal))
        {
            if (!definitionsByPair.TryGetValue(ratePoint.Pair, out var definition))
                continue;

            items.Add(CreateFXRate(definition, ratePoint));
        }

        var lastEvent = definitions
            .Select(model => (model.ValidFrom, model.LastAuditDateTime, model.LastEventID))
            .Concat(ratePoints.Select(model => (model.ValidFrom, model.LastAuditDateTime, model.LastEventID)))
            .OrderBy(model => model.ValidFrom)
            .ThenBy(model => model.LastAuditDateTime)
            .ThenBy(model => model.LastEventID)
            .LastOrDefault();

        var lastAuditDateTime = definitions
            .Select(model => (DateTime?)model.LastAuditDateTime)
            .Concat(ratePoints.Select(model => (DateTime?)model.LastAuditDateTime))
            .Max() ?? DateTime.UtcNow;

        return new FXRates(
            valuationDateTime,
            AuditDateTimeBuilder.Create(lastAuditDateTime),
            lastEvent.LastEventID == Guid.Empty ? Constants.Initialisation.EmptyViewEventID : new EventID(lastEvent.LastEventID),
            LastAuditDateTimeBuilder.Create(lastAuditDateTime),
            items);
    }

    public async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        var fxEvents = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId, cancellationToken);
        var rateEvents = await eventRepository.LoadStreamAsync<IFXRateEvent>(Constants.Initialisation.FXRatesStreamId, cancellationToken);

        await StoreProjectionAsync(BuildDefinitionModels(fxEvents, null), BuildRatePointModels(rateEvents, null), null, cancellationToken);
    }

    public async Task RebuildPairAsync(CurrencyPair pair, CancellationToken cancellationToken = default)
    {
        if (pair is null)
            throw new ArgumentNullException(nameof(pair));

        var fxEvents = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId, cancellationToken);
        var rateEvents = await eventRepository.LoadStreamAsync<IFXRateEvent>(Constants.Initialisation.FXRatesStreamId, cancellationToken);

        await StoreProjectionAsync(BuildDefinitionModels(fxEvents, pair), BuildRatePointModels(rateEvents, pair), pair.Value, cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var session = store.LightweightSession();
        session.DeleteWhere<FXDefinitionReadModel>(_ => true);
        session.DeleteWhere<FXRatePointReadModel>(_ => true);
        await session.SaveChangesAsync(cancellationToken);
    }

    private async Task StoreProjectionAsync(IReadOnlyList<FXDefinitionReadModel> definitions, IReadOnlyList<FXRatePointReadModel> ratePoints, string? pair, CancellationToken cancellationToken)
    {
        await using (var deleteSession = store.LightweightSession())
        {
            if (pair is null)
            {
                deleteSession.DeleteWhere<FXDefinitionReadModel>(_ => true);
                deleteSession.DeleteWhere<FXRatePointReadModel>(_ => true);
            }
            else
            {
                deleteSession.DeleteWhere<FXDefinitionReadModel>(model => model.Pair == pair);
                deleteSession.DeleteWhere<FXRatePointReadModel>(model => model.Pair == pair);
            }

            await deleteSession.SaveChangesAsync(cancellationToken);
        }

        foreach (var batch in definitions.Cast<object>().Concat(ratePoints).Chunk(StoreBatchSize))
        {
            await using var session = store.LightweightSession();
            foreach (var model in batch)
                session.Store(model);

            await session.SaveChangesAsync(cancellationToken);
        }
    }

    private static IReadOnlyList<FXDefinitionReadModel> BuildDefinitionModels(IReadOnlyList<IFXEvent> events, CurrencyPair? pair)
    {
        var filteredEvents = events
            .Where(@event => pair is null || GetPair(@event) == pair)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        var models = new List<FXDefinitionReadModel>();
        foreach (var group in filteredEvents.GroupBy(GetPair))
        {
            FX? current = null;
            var orderedEvents = group.ToList();

            for (var index = 0; index < orderedEvents.Count; index++)
            {
                current = Apply(current, orderedEvents[index]);
                var validTo = index + 1 < orderedEvents.Count ? orderedEvents[index + 1].EventDateTime.Value : (DateTime?)null;
                models.Add(CreateDefinitionModel(current, validTo));
            }
        }

        return models;
    }

    private static IReadOnlyList<FXRatePointReadModel> BuildRatePointModels(IReadOnlyList<IFXRateEvent> events, CurrencyPair? pair)
    {
        var filteredEvents = events
            .OfType<FXRateSetEvent>()
            .Where(@event => pair is null || @event.Pair == pair)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        var models = new List<FXRatePointReadModel>();
        foreach (var group in filteredEvents.GroupBy(@event => @event.Pair))
        {
            var orderedEvents = group.ToList();
            for (var index = 0; index < orderedEvents.Count; index++)
            {
                var validTo = index + 1 < orderedEvents.Count ? orderedEvents[index + 1].EventDateTime.Value : (DateTime?)null;
                models.Add(CreateRatePointModel(orderedEvents[index], validTo));
            }
        }

        return models;
    }

    private static FX Apply(FX? current, IFXEvent @event) =>
        @event switch
        {
            FXCreatedEvent createdEvent => FXBuilder.Create(createdEvent),
            FXActiveModifiedEvent activeModifiedEvent when current is not null => current.Apply(activeModifiedEvent),
            FXActiveModifiedEvent activeModifiedEvent => throw new InvalidOperationException($"No matching FX found for Pair '{activeModifiedEvent.Pair}'."),
            _ => throw new InvalidOperationException($"Unsupported FX event type '{@event.GetType().Name}'.")
        };

    private static FXDefinitionReadModel CreateDefinitionModel(FX fx, DateTime? validTo) =>
        new()
        {
            Id = $"{fx.Pair.Value}|{fx.ValuationDateTime.Value:O}|{fx.LastEventID.Value:N}",
            Pair = fx.Pair.Value,
            BaseCurrency = fx.BaseCurrency.Value,
            QuoteCurrency = fx.QuoteCurrency.Value,
            DisplayPair = fx.DisplayPair,
            Active = fx.Active,
            ValidFrom = ToDatabaseDateTime(fx.ValuationDateTime.Value),
            ValidTo = ToDatabaseDateTime(validTo),
            LastEventID = fx.LastEventID.Value,
            LastAuditDateTime = ToDatabaseDateTime(fx.LastAuditDateTime.Value)
        };

    private static FXRatePointReadModel CreateRatePointModel(FXRateSetEvent @event, DateTime? validTo) =>
        new()
        {
            Id = $"{@event.Pair.Value}|{@event.EventDateTime.Value:O}|{@event.EventID.Value:N}",
            Pair = @event.Pair.Value,
            Bid = @event.Price.Bid.Value,
            Mid = @event.Price.Mid.Value,
            Ask = @event.Price.Ask.Value,
            ValidFrom = ToDatabaseDateTime(@event.EventDateTime.Value),
            ValidTo = ToDatabaseDateTime(validTo),
            LastEventID = @event.EventID.Value,
            LastAuditDateTime = ToDatabaseDateTime(@event.AuditDateTime.Value)
        };

    private static FXRate CreateFXRate(FXDefinitionReadModel definition, FXRatePointReadModel ratePoint) =>
        new(
            new CurrencyPair(Alpha3Builder.Create(definition.BaseCurrency), Alpha3Builder.Create(definition.QuoteCurrency)),
            Alpha3Builder.Create(definition.BaseCurrency),
            Alpha3Builder.Create(definition.QuoteCurrency),
            definition.DisplayPair,
            definition.Active,
            new FXPrice(new Bid(ratePoint.Bid), new Mid(ratePoint.Mid), new Ask(ratePoint.Ask)),
            EventDateTimeBuilder.Create(ratePoint.ValidFrom),
            AuditDateTimeBuilder.Create(ratePoint.LastAuditDateTime),
            new EventID(ratePoint.LastEventID),
            LastAuditDateTimeBuilder.Create(ratePoint.LastAuditDateTime));

    private static CurrencyPair GetPair(IFXEvent @event) =>
        @event switch
        {
            FXCreatedEvent createdEvent => createdEvent.Pair,
            FXActiveModifiedEvent activeModifiedEvent => activeModifiedEvent.Pair,
            _ => throw new InvalidOperationException($"Unsupported FX event type '{@event.GetType().Name}'.")
        };

    private static DateTime ToDatabaseDateTime(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private static DateTime ToDatabaseDateTime(DateTime? value) =>
        value.HasValue ? ToDatabaseDateTime(value.Value) : DateTime.MaxValue;
}
