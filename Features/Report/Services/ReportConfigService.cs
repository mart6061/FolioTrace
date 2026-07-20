using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class ReportConfigService(IEventRepository eventRepository, int cacheCapacity = 500) : IReferenceDataService<ReportConfigs, ReportConfigServiceDiagnostics>
{
    private readonly Lock cacheLock = new();
    private readonly BoundedLruCache<ReportConfigCacheKey, ReportConfigs> cache = new(cacheCapacity);

    public Task<ReportConfigs> Current => Get(ReferenceDataCurrent.EndOfToday());

    public ReportConfigServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var reportCount = cache.Values
                .OrderByDescending(reports => reports.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new ReportConfigServiceDiagnostics(cache.Count, reportCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(IReportEvent @event) => InvalidateAll();

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = ReportConfigCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task<ReportConfigs> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = ReportConfigCacheKey.ForAllAuditHistory(valuationDate);
        var lastEventID = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.ReportConfigsStreamId);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached) && cached.LastEventID == lastEventID)
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IReportEvent>(Constants.Initialisation.ReportConfigsStreamId);
        var current = new ReportConfigs(valuationDate, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<ReportConfigs> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = ReportConfigCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IReportEvent>(Constants.Initialisation.ReportConfigsStreamId);
        var current = new ReportConfigs(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct ReportConfigCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static ReportConfigCacheKey ForAllAuditHistory(EventDateTime valuationDate) =>
            new(valuationDate.Value, null);

        public static ReportConfigCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) =>
            new(valuationDate.Value, asAt.Value);
    }

}
