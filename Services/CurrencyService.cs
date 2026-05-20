using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class CurrencyService(IEventRepository eventRepository)
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<CurrencyCacheKey, Currencies> cache = [];

    public CurrencyServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var currencyCount = cache.Values
                .OrderByDescending(currencies => currencies.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new CurrencyServiceDiagnostics(cache.Count, currencyCount);
        }
    }

    public int Invalidate(CurrencyCreatedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public int Invalidate(CurrencyModifiedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public async Task<Currencies> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = CurrencyCacheKey.ForAllAuditHistory(valuationDate);
        var lastEventID = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.CurrenciesStreamId, valuationDate.Value);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached) && cached.LastEventID == lastEventID)
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<ICurrencyEvent>(Constants.Initialisation.CurrenciesStreamId);
        var current = new Currencies(valuationDate, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<Currencies> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = CurrencyCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<ICurrencyEvent>(Constants.Initialisation.CurrenciesStreamId);
        var current = new Currencies(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct CurrencyCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static CurrencyCacheKey ForAllAuditHistory(EventDateTime valuationDate) =>
            new(valuationDate.Value, null);

        public static CurrencyCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) =>
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
