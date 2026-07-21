using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;
using System.Text.Json;

namespace Services;

public sealed class InstrumentService(IEventRepository eventRepository, int cacheCapacity = 500, IAggregateSnapshotRepository? snapshotRepository = null) : IReferenceDataService<Instruments, InstrumentServiceDiagnostics>
{
    private const string AggregateKind = "Instruments";
    private readonly Lock cacheLock = new();
    private readonly BoundedLruCache<InstrumentCacheKey, Instruments> cache = new(cacheCapacity);

    public Task<Instruments> Current => Get(ReferenceDataCurrent.EndOfToday());

    public InstrumentServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var instrumentCount = cache.Values
                .OrderByDescending(instruments => instruments.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new InstrumentServiceDiagnostics(cache.Count, instrumentCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(IInstrumentEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = InstrumentCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task<Instruments> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = InstrumentCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task PersistSnapshotAsync(Instruments current, CancellationToken cancellationToken = default)
    {
        if (snapshotRepository is null)
            return;

        await snapshotRepository.SaveAsync(new AggregateSnapshot
        {
            Id = Guid.CreateGuid7(), AggregateKind = AggregateKind, StreamId = Constants.Initialisation.InstrumentsStreamId,
            ValuationDateTime = current.ValuationDateTime.Value, AsOfDateTime = current.AsOfDateTime.Value,
            LastEventID = current.LastEventID.Value, LastAuditDateTime = current.LastAuditDateTime.Value,
            PayloadJson = JsonSerializer.Serialize(current.Items), CreatedAtUtc = DateTime.UtcNow,
            SourceEventCount = current.Items.Count, Superseded = false
        }, cancellationToken);
    }

    private async Task<Instruments> BuildCurrent(EventDateTime valuationDate)
    {
        var snapshot = snapshotRepository is null ? null : await snapshotRepository.FindLatestAsync(AggregateKind, Constants.Initialisation.InstrumentsStreamId, valuationDate.Value);
        if (snapshot is null)
        {
            var events = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId);
            return new Instruments(valuationDate, events.ToList());
        }

        var delta = (await eventRepository.LoadStreamAfterAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId, new EventID(snapshot.LastEventID)))
            .Where(@event => @event.EventDateTime.Value <= valuationDate.Value).ToList();
        var asOf = delta.Count == 0 ? snapshot.AsOfDateTime : delta.Max(@event => @event.AuditDateTime.Value);
        return new Instruments(valuationDate, new AuditDateTime(asOf), new EventID(snapshot.LastEventID), new LastAuditDateTime(snapshot.LastAuditDateTime),
            JsonSerializer.Deserialize<List<Instrument>>(snapshot.PayloadJson) ?? [], delta);
    }

    public async Task<Instruments> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = InstrumentCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId);
        var current = new Instruments(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct InstrumentCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static InstrumentCacheKey ForAllAuditHistory(EventDateTime valuationDate) => new(valuationDate.Value, null);
        public static InstrumentCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) => new(valuationDate.Value, asAt.Value);
    }

    private int InvalidateFrom(EventDateTime eventDateTime)
    {
        snapshotRepository?.RetireFromAsync(AggregateKind, Constants.Initialisation.InstrumentsStreamId, eventDateTime.Value).GetAwaiter().GetResult();
        lock (cacheLock)
        {
            var removedCount = 0;
            foreach (var cacheKey in cache.Keys.Where(cacheKey => !cacheKey.AsAtDateTime.HasValue && cacheKey.ValuationDateTime >= eventDateTime.Value).ToList())
            {
                if (cache.Remove(cacheKey))
                    removedCount++;
            }

            return removedCount;
        }
    }
}
