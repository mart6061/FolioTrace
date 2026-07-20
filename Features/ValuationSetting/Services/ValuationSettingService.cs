using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class ValuationSettingService(IEventRepository eventRepository, int cacheCapacity = 500) : IReferenceDataService<ValuationSettings, ValuationSettingServiceDiagnostics>
{
    private readonly Lock cacheLock = new();
    private readonly BoundedLruCache<ValuationSettingCacheKey, ValuationSettings> cache = new(cacheCapacity);

    public Task<ValuationSettings> Current => Get(ReferenceDataCurrent.EndOfToday());

    public ValuationSettingServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var valuationSettingCount = cache.Values
                .OrderByDescending(settings => settings.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new ValuationSettingServiceDiagnostics(cache.Count, valuationSettingCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(IValuationSettingEvent @event) => InvalidateAll();

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = ValuationSettingCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task<ValuationSettings> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = ValuationSettingCacheKey.ForAllAuditHistory(valuationDate);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IValuationSettingEvent>(Constants.Initialisation.ValuationSettingsStreamId);
        var current = new ValuationSettings(valuationDate, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<ValuationSettings> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = ValuationSettingCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IValuationSettingEvent>(Constants.Initialisation.ValuationSettingsStreamId);
        var current = new ValuationSettings(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct ValuationSettingCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static ValuationSettingCacheKey ForAllAuditHistory(EventDateTime valuationDate) =>
            new(valuationDate.Value, null);

        public static ValuationSettingCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) =>
            new(valuationDate.Value, asAt.Value);
    }

}
