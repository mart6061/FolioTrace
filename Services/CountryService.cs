using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class CountryService(IEventRepository eventRepository)
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<CountryCacheKey, Countries> cache = [];

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
}
