using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class FXRateService(IEventRepository eventRepository)
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<FXRateCacheKey, FXRates> cache = [];

    public FXRateServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var rateCount = cache.Values
                .OrderByDescending(rates => rates.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new FXRateServiceDiagnostics(cache.Count, rateCount);
        }
    }

    public int Invalidate(FXCreatedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public int Invalidate(FXActiveModifiedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public int Invalidate(FXRateSetEvent @event) => InvalidateFrom(@event.EventDateTime);

    public int InvalidateAll()
    {
        lock (cacheLock)
        {
            var removedCount = cache.Count;
            cache.Clear();
            return removedCount;
        }
    }

    public async Task<FXRates> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = FXRateCacheKey.ForAllAuditHistory(valuationDate);
        var lastEventID = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.FXRatesStreamId, valuationDate.Value);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached) && cached.LastEventID == (lastEventID ?? Constants.Initialisation.EmptyViewEventID))
                return cached;
        }

        var fxEvents = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId);
        var rateEvents = await eventRepository.LoadStreamAsync<IFXRateEvent>(Constants.Initialisation.FXRatesStreamId);
        var current = new FXRates(valuationDate, fxEvents.ToList(), rateEvents.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<FXRates> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = FXRateCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var fxEvents = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId);
        var rateEvents = await eventRepository.LoadStreamAsync<IFXRateEvent>(Constants.Initialisation.FXRatesStreamId);
        var current = new FXRates(valuationDate, asAt, fxEvents.ToList(), rateEvents.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct FXRateCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static FXRateCacheKey ForAllAuditHistory(EventDateTime valuationDate) =>
            new(valuationDate.Value, null);

        public static FXRateCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) =>
            new(valuationDate.Value, asAt.Value);
    }

    private int InvalidateFrom(EventDateTime eventDateTime)
    {
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
