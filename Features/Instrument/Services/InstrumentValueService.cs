using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Snapshots;
using FolioTrace.Types;
using Repository;
using System.Text.Json;

namespace Services;

public sealed class InstrumentValueService(IEventRepository eventRepository, int cacheCapacity = 500, IAggregateSnapshotRepository? snapshotRepository = null, InstrumentService? instrumentService = null)
{
    private const string AggregateKind = "InstrumentValues";
    private readonly Lock cacheLock = new();
    private readonly BoundedLruCache<InstrumentValueCacheKey, InstrumentValues> cache = new(cacheCapacity);

    public InstrumentValueServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var instrumentValueCount = cache.Values
                .OrderByDescending(values => values.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new InstrumentValueServiceDiagnostics(cache.Count, instrumentValueCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(IInstrumentEvent @event) => InvalidateFrom(@event.EventDateTime);
    public int Invalidate(IInstrumentPriceEvent @event) => InvalidateFrom(@event.EventDateTime);
    public int Invalidate(IInstrumentIncomeEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = InstrumentValueCacheKey.ForAllAuditHistory(valuationDate);

        lock (cacheLock)
        {
            return cache.ContainsKey(cacheKey);
        }
    }

    public int InvalidateAll()
    {
        lock (cacheLock)
        {
            var removedCount = cache.Count;
            cache.Clear();
            return removedCount;
        }
    }

    public async Task<InstrumentValues> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = InstrumentValueCacheKey.ForAllAuditHistory(valuationDate);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var current = await BuildCurrent(valuationDate);

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task PersistSnapshotAsync(InstrumentValues current, CancellationToken cancellationToken = default)
    {
        if (snapshotRepository is null)
            return;

        var priceCheckpoint = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.InstrumentPricesStreamId, current.ValuationDateTime.Value, cancellationToken: cancellationToken)
            ?? Constants.Initialisation.EmptyViewEventID;
        var incomeCheckpoint = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.InstrumentIncomesStreamId, current.ValuationDateTime.Value, cancellationToken: cancellationToken)
            ?? Constants.Initialisation.EmptyViewEventID;
        var payload = new InstrumentValueSnapshotPayload(current.Items, priceCheckpoint.Value, incomeCheckpoint.Value);
        await snapshotRepository.SaveAsync(new AggregateSnapshot
        {
            Id = Guid.CreateGuid7(), AggregateKind = AggregateKind, StreamId = Constants.Initialisation.InstrumentPricesStreamId,
            ValuationDateTime = current.ValuationDateTime.Value, AsOfDateTime = current.AsOfDateTime.Value,
            LastEventID = priceCheckpoint.Value, LastAuditDateTime = current.LastAuditDateTime.Value,
            PayloadJson = JsonSerializer.Serialize(payload), CreatedAtUtc = DateTime.UtcNow,
            SourceEventCount = current.Items.Count, Superseded = false
        }, cancellationToken);
    }

    private async Task<InstrumentValues> BuildCurrent(EventDateTime valuationDate)
    {
        var snapshot = snapshotRepository is null ? null : await snapshotRepository.FindLatestAsync(AggregateKind, Constants.Initialisation.InstrumentPricesStreamId, valuationDate.Value);
        if (snapshot is null)
        {
            var instrumentEvents = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId);
            var priceEvents = await eventRepository.LoadStreamAsync<IInstrumentPriceEvent>(Constants.Initialisation.InstrumentPricesStreamId);
            var incomeEvents = await eventRepository.LoadStreamAsync<IInstrumentIncomeEvent>(Constants.Initialisation.InstrumentIncomesStreamId);
            return new InstrumentValues(valuationDate, instrumentEvents.ToList(), priceEvents.ToList(), incomeEvents.ToList());
        }

        var payload = JsonSerializer.Deserialize<InstrumentValueSnapshotPayload>(snapshot.PayloadJson)
            ?? throw new InvalidOperationException("Instrument value snapshot payload is invalid.");
        var instruments = instrumentService is null
            ? new Instruments(valuationDate, (await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId)).ToList())
            : await instrumentService.Get(valuationDate);
        var priceDelta = (await eventRepository.LoadStreamAfterAsync<IInstrumentPriceEvent>(Constants.Initialisation.InstrumentPricesStreamId, new EventID(payload.PriceCheckpoint)))
            .Where(@event => @event.EventDateTime.Value <= valuationDate.Value).ToList();
        var incomeDelta = (await eventRepository.LoadStreamAfterAsync<IInstrumentIncomeEvent>(Constants.Initialisation.InstrumentIncomesStreamId, new EventID(payload.IncomeCheckpoint)))
            .Where(@event => @event.EventDateTime.Value <= valuationDate.Value).ToList();
        var asOf = new[] { snapshot.AsOfDateTime, instruments.LastAuditDateTime.Value,
            priceDelta.Select(@event => @event.AuditDateTime.Value).DefaultIfEmpty(snapshot.AsOfDateTime).Max(),
            incomeDelta.Select(@event => @event.AuditDateTime.Value).DefaultIfEmpty(snapshot.AsOfDateTime).Max() }.Max();
        return new InstrumentValues(valuationDate, new AuditDateTime(asOf), instruments, payload.Items, priceDelta, incomeDelta);
    }

    public async Task<InstrumentValues> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = InstrumentValueCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var instrumentEvents = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId);
        var priceEvents = await eventRepository.LoadStreamAsync<IInstrumentPriceEvent>(Constants.Initialisation.InstrumentPricesStreamId);
        var incomeEvents = await eventRepository.LoadStreamAsync<IInstrumentIncomeEvent>(Constants.Initialisation.InstrumentIncomesStreamId);
        var current = new InstrumentValues(valuationDate, asAt, instrumentEvents.ToList(), priceEvents.ToList(), incomeEvents.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct InstrumentValueCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static InstrumentValueCacheKey ForAllAuditHistory(EventDateTime valuationDate) => new(valuationDate.Value, null);
        public static InstrumentValueCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) => new(valuationDate.Value, asAt.Value);
    }

    private int InvalidateFrom(EventDateTime eventDateTime)
    {
        snapshotRepository?.RetireFromAsync(AggregateKind, Constants.Initialisation.InstrumentPricesStreamId, eventDateTime.Value).GetAwaiter().GetResult();
        lock (cacheLock)
        {
            var removedCount = 0;
            foreach (var cacheKey in cache.Keys.Where(cacheKey => cacheKey.ValuationDateTime >= eventDateTime.Value).ToList())
            {
                if (cache.Remove(cacheKey))
                    removedCount++;
            }

            return removedCount;
        }
    }

    private sealed record InstrumentValueSnapshotPayload(List<InstrumentValue> Items, Guid PriceCheckpoint, Guid IncomeCheckpoint);
}
