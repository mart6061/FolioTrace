using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class CountryService(IEventRepository eventRepository) : IReferenceDataService<Countries, CountryServiceDiagnostics>
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<CountryCacheKey, Countries> cache = [];

    public Task<Countries> Current => Get(ReferenceDataCurrent.EndOfToday());

    public CountryServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var countryCount = cache.Values
                .OrderByDescending(countries => countries.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new CountryServiceDiagnostics(cache.Count, countryCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(CountryCreatedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public int Invalidate(CountryModifiedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public int Invalidate(CountryFlagModifiedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = CountryCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task<Countries> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = CountryCacheKey.ForAllAuditHistory(valuationDate);
        var lastEventID = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.CountriesStreamId, valuationDate.Value);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached) && cached.LastEventID == lastEventID)
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<ICountryEvent>(Constants.Initialisation.CountriesStreamId);
        var current = new Countries(valuationDate, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<Countries> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = CountryCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<ICountryEvent>(Constants.Initialisation.CountriesStreamId);
        var current = new Countries(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct CountryCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static CountryCacheKey ForAllAuditHistory(EventDateTime valuationDate) =>
            new(valuationDate.Value, null);

        public static CountryCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) =>
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
