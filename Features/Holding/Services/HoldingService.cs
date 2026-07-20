using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class HoldingService(IEventRepository eventRepository)
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<HoldingCacheKey, Holdings> cache = [];

    public HoldingServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var holdingCount = cache.Values
                .OrderByDescending(holdings => holdings.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new HoldingServiceDiagnostics(cache.Count, holdingCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(IHoldingEvent @event) => InvalidateFrom(@event.EventDateTime);
    public int Invalidate(HoldingActiveModifiedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = HoldingCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task<Holdings> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = HoldingCacheKey.ForAllAuditHistory(valuationDate);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IHoldingEvent>(Constants.Initialisation.HoldingsStreamId);
        var current = new Holdings(valuationDate, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<Holdings> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = HoldingCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IHoldingEvent>(Constants.Initialisation.HoldingsStreamId);
        var current = new Holdings(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct HoldingCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static HoldingCacheKey ForAllAuditHistory(EventDateTime valuationDate) => new(valuationDate.Value, null);
        public static HoldingCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) => new(valuationDate.Value, asAt.Value);
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
