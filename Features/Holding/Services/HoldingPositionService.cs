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

    public int Invalidate(IEventBase @event) => InvalidateFrom(GetInvalidationDate(@event));

    public bool IsCached(EventDateTime valuationDate) => IsCached(valuationDate, HoldingDateBasis.EventDateTime);

    public bool IsCached(EventDateTime valuationDate, HoldingDateBasis holdingDateBasis)
    {
        var cacheKey = HoldingPositionCacheKey.ForAllAuditHistory(valuationDate, holdingDateBasis);
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

    public Task<HoldingPositions> Get(EventDateTime valuationDate) =>
        Get(valuationDate, HoldingDateBasis.EventDateTime);

    public async Task<HoldingPositions> Get(EventDateTime valuationDate, HoldingDateBasis holdingDateBasis)
    {
        var cacheKey = HoldingPositionCacheKey.ForAllAuditHistory(valuationDate, holdingDateBasis);
        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var asAt = AuditDateTimeBuilder.Create();
        var current = await Build(valuationDate, asAt, HoldingPositionFilter.Default, holdingDateBasis);
        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public Task<HoldingPositions> Get(EventDateTime valuationDate, AuditDateTime asAt, HoldingPositionFilter filter) =>
        Get(valuationDate, asAt, filter, HoldingDateBasis.EventDateTime);

    public async Task<HoldingPositions> Get(EventDateTime valuationDate, AuditDateTime asAt, HoldingPositionFilter filter, HoldingDateBasis holdingDateBasis)
    {
        var cacheKey = HoldingPositionCacheKey.ForFilter(valuationDate, asAt, filter, holdingDateBasis);
        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var current = await Build(valuationDate, asAt, filter, holdingDateBasis);
        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private async Task<HoldingPositions> Build(EventDateTime valuationDate, AuditDateTime asAt, HoldingPositionFilter filter, HoldingDateBasis holdingDateBasis)
    {
        var holdings = await holdingService.Get(valuationDate, asAt);
        var accounts = await accountService.Get(valuationDate, asAt);
        var instruments = await instrumentService.Get(valuationDate, asAt);
        var transactionEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId);
        return new HoldingPositions(valuationDate, asAt, holdings, accounts, instruments, transactionEvents, filter, holdingDateBasis);
    }

    private int InvalidateFrom(DateTime eventDateTime)
    {
        lock (cacheLock)
        {
            var removedCount = 0;
            foreach (var cacheKey in cache.Keys.Where(cacheKey => cacheKey.ValuationDateTime >= eventDateTime).ToList())
            {
                if (cache.Remove(cacheKey))
                    removedCount++;
            }

            return removedCount;
        }
    }

    private readonly record struct HoldingPositionCacheKey(
        DateTime ValuationDateTime,
        HoldingDateBasis HoldingDateBasis,
        DateTime? AsAtDateTime,
        Guid? HoldingID,
        Guid? AccountID,
        Guid? InstrumentID,
        bool IncludeExcluded,
        bool IncludeZero)
    {
        public static HoldingPositionCacheKey ForAllAuditHistory(EventDateTime valuationDate, HoldingDateBasis holdingDateBasis) =>
            new(valuationDate.Value, holdingDateBasis, null, null, null, null, false, false);

        public static HoldingPositionCacheKey ForFilter(EventDateTime valuationDate, AuditDateTime asAt, HoldingPositionFilter filter, HoldingDateBasis holdingDateBasis) =>
            new(
                valuationDate.Value,
                holdingDateBasis,
                asAt.Value,
                filter.HoldingID?.Value,
                filter.AccountID?.Value,
                filter.InstrumentID?.Value,
                filter.IncludeExcluded,
                filter.IncludeZero);
    }

    private static DateTime GetInvalidationDate(IEventBase @event) =>
        @event is ITransactionEvent transactionEvent
            ? new[] { transactionEvent.EventDateTime.Value, transactionEvent.SettlementDateTime.Value }.Min()
            : @event.EventDateTime.Value;
}

public sealed record HoldingPositionServiceDiagnostics(int CacheEntryCount, int PositionCount, long EstimatedMemoryBytes);
