using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class FXService(IEventRepository eventRepository) : IReferenceDataService<FXs, FXServiceDiagnostics>
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<FXCacheKey, FXs> cache = [];

    public Task<FXs> Current => Get(ReferenceDataCurrent.EndOfToday());

    public FXServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var fxCount = cache.Values
                .OrderByDescending(fxs => fxs.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new FXServiceDiagnostics(cache.Count, fxCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(FXCreatedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public int Invalidate(FXActiveModifiedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = FXCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task<FXs> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = FXCacheKey.ForAllAuditHistory(valuationDate);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId);
        var current = new FXs(valuationDate, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<FXs> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = FXCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId);
        var current = new FXs(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct FXCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static FXCacheKey ForAllAuditHistory(EventDateTime valuationDate) =>
            new(valuationDate.Value, null);

        public static FXCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) =>
            new(valuationDate.Value, asAt.Value);
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
