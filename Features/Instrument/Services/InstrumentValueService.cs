using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class InstrumentValueService(IEventRepository eventRepository)
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<InstrumentValueCacheKey, InstrumentValues> cache = [];

    public InstrumentValueServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var instrumentValueCount = cache.Values
                .OrderByDescending(values => values.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new InstrumentValueServiceDiagnostics(cache.Count, instrumentValueCount);
        }
    }

    public int Invalidate(IInstrumentEvent @event) => InvalidateFrom(@event.EventDateTime);
    public int Invalidate(IInstrumentPriceEvent @event) => InvalidateFrom(@event.EventDateTime);
    public int Invalidate(IInstrumentIncomeEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = InstrumentValueCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task<InstrumentValues> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = InstrumentValueCacheKey.ForAllAuditHistory(valuationDate);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var instrumentEvents = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId);
        var priceEvents = await eventRepository.LoadStreamAsync<IInstrumentPriceEvent>(Constants.Initialisation.InstrumentPricesStreamId);
        var incomeEvents = await eventRepository.LoadStreamAsync<IInstrumentIncomeEvent>(Constants.Initialisation.InstrumentIncomesStreamId);
        var current = new InstrumentValues(valuationDate, instrumentEvents.ToList(), priceEvents.ToList(), incomeEvents.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<InstrumentValues> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = InstrumentValueCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var instrumentEvents = await eventRepository.LoadStreamAsync<IInstrumentEvent>(Constants.Initialisation.InstrumentsStreamId);
        var priceEvents = await eventRepository.LoadStreamAsync<IInstrumentPriceEvent>(Constants.Initialisation.InstrumentPricesStreamId);
        var incomeEvents = await eventRepository.LoadStreamAsync<IInstrumentIncomeEvent>(Constants.Initialisation.InstrumentIncomesStreamId);
        var current = new InstrumentValues(valuationDate, asAt, instrumentEvents.ToList(), priceEvents.ToList(), incomeEvents.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct InstrumentValueCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static InstrumentValueCacheKey ForAllAuditHistory(EventDateTime valuationDate) => new(valuationDate.Value, null);
        public static InstrumentValueCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) => new(valuationDate.Value, asAt.Value);
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

public sealed record InstrumentValueServiceDiagnostics(int CacheEntryCount, int InstrumentValueCount);
