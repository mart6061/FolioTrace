using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class HoldingPositionService(
    IEventRepository eventRepository,
    HoldingService holdingService,
    AccountService accountService,
    InstrumentService instrumentService)
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<HoldingPositionCacheKey, HoldingPositions> cache = [];

    public HoldingPositionServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var positionCount = cache.Values
                .OrderByDescending(positions => positions.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new HoldingPositionServiceDiagnostics(cache.Count, positionCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(IEventBase @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        var cacheKey = HoldingPositionCacheKey.ForAllAuditHistory(valuationDate);
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

    public async Task<HoldingPositions> Get(EventDateTime valuationDate)
    {
        var cacheKey = HoldingPositionCacheKey.ForAllAuditHistory(valuationDate);
        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var asAt = AuditDateTimeBuilder.Create();
        var current = await Build(valuationDate, asAt, HoldingPositionFilter.Default);
        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<HoldingPositions> Get(EventDateTime valuationDate, AuditDateTime asAt, HoldingPositionFilter filter)
    {
        var cacheKey = HoldingPositionCacheKey.ForFilter(valuationDate, asAt, filter);
        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var current = await Build(valuationDate, asAt, filter);
        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private async Task<HoldingPositions> Build(EventDateTime valuationDate, AuditDateTime asAt, HoldingPositionFilter filter)
    {
        var holdings = await holdingService.Get(valuationDate, asAt);
        var accounts = await accountService.Get(valuationDate, asAt);
        var instruments = await instrumentService.Get(valuationDate, asAt);
        var transactionEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId);
        return new HoldingPositions(valuationDate, asAt, holdings, accounts, instruments, transactionEvents, filter);
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

    private readonly record struct HoldingPositionCacheKey(
        DateTime ValuationDateTime,
        DateTime? AsAtDateTime,
        Guid? HoldingID,
        Guid? AccountID,
        Guid? InstrumentID,
        bool IncludeExcluded,
        bool IncludeZero)
    {
        public static HoldingPositionCacheKey ForAllAuditHistory(EventDateTime valuationDate) =>
            new(valuationDate.Value, null, null, null, null, false, false);

        public static HoldingPositionCacheKey ForFilter(EventDateTime valuationDate, AuditDateTime asAt, HoldingPositionFilter filter) =>
            new(
                valuationDate.Value,
                asAt.Value,
                filter.HoldingID?.Value,
                filter.AccountID?.Value,
                filter.InstrumentID?.Value,
                filter.IncludeExcluded,
                filter.IncludeZero);
    }
}

public sealed record HoldingPositionServiceDiagnostics(int CacheEntryCount, int PositionCount, long EstimatedMemoryBytes);
