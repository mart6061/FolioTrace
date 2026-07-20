using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class InstrumentService(IEventRepository eventRepository, int cacheCapacity = 500) : IReferenceDataService<Instruments, InstrumentServiceDiagnostics>
{
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

        var events = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId);
        var current = new Instruments(valuationDate, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
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
