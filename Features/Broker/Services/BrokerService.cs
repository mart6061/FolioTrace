using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class BrokerService(IEventRepository eventRepository, int cacheCapacity = 500) : IReferenceDataService<Brokers, BrokerServiceDiagnostics>
{
    private readonly Lock cacheLock = new();
    private readonly BoundedLruCache<BrokerCacheKey, Brokers> cache = new(cacheCapacity);

    public Task<Brokers> Current => Get(ReferenceDataCurrent.EndOfToday());

    public BrokerServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var brokerCount = cache.Values
                .OrderByDescending(brokers => brokers.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new BrokerServiceDiagnostics(cache.Count, brokerCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(IBrokerEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = BrokerCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task<Brokers> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = BrokerCacheKey.ForAllAuditHistory(valuationDate);
        var lastEventID = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.BrokersStreamId, valuationDate.Value);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached) && cached.LastEventID == (lastEventID ?? Constants.Initialisation.EmptyViewEventID))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IBrokerEvent>(Constants.Initialisation.BrokersStreamId);
        var current = new Brokers(valuationDate, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<Brokers> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = BrokerCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IBrokerEvent>(Constants.Initialisation.BrokersStreamId);
        var current = new Brokers(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct BrokerCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static BrokerCacheKey ForAllAuditHistory(EventDateTime valuationDate) =>
            new(valuationDate.Value, null);

        public static BrokerCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) =>
            new(valuationDate.Value, asAt.Value);
    }

    private int InvalidateFrom(EventDateTime eventDateTime)
    {
        if (eventDateTime is null)
            throw new ArgumentNullException(nameof(eventDateTime));

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
}
