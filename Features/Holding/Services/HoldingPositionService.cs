using System.Text.Json;
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
    InstrumentService instrumentService,
    IAggregateSnapshotRepository snapshotRepository,
    int cacheCapacity = 2000)
{
    private const string AggregateKind = "HoldingPositions";

    private readonly Lock cacheLock = new();
    private readonly BoundedLruCache<HoldingPositionCacheKey, HoldingPositions> cache = new(cacheCapacity);

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
        var current = await BuildCurrent(valuationDate, asAt, holdingDateBasis);
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

    /// <summary>
    /// Persists a snapshot of an already-computed "current" HoldingPositions instance. Called by
    /// AggregateMaintenanceCoordinator's warm loop (Aggregate-Snapshot-Scaling-Plan.md 3.2) once a boundary
    /// has aged into the warm/cold tier - not on every read.
    /// </summary>
    public async Task PersistSnapshotAsync(HoldingPositions positions, CancellationToken cancellationToken = default)
    {
        if (positions is null)
            throw new ArgumentNullException(nameof(positions));

        var payload = positions.Items
            .Select(item => new HoldingPositionSnapshotItem(item.HoldingID.Value, item.Quantity, item.BookCost, item.LastEventID.Value, item.LastAuditDateTime.Value))
            .ToList();

        var snapshot = new AggregateSnapshot
        {
            Id = Guid.CreateGuid7(),
            AggregateKind = AggregateKind,
            StreamId = Constants.Initialisation.TransactionsStreamId,
            Variant = positions.HoldingDateBasis.ToString(),
            ValuationDateTime = positions.ValuationDateTime.Value,
            AsOfDateTime = positions.AsOfDateTime.Value,
            LastEventID = positions.LastEventID.Value,
            LastAuditDateTime = positions.LastAuditDateTime.Value,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAtUtc = DateTime.UtcNow,
            SourceEventCount = payload.Count
        };

        await snapshotRepository.SaveAsync(snapshot, cancellationToken);
    }

    private async Task<HoldingPositions> Build(EventDateTime valuationDate, AuditDateTime asAt, HoldingPositionFilter filter, HoldingDateBasis holdingDateBasis)
    {
        var holdings = await holdingService.Get(valuationDate, asAt);
        var accounts = await accountService.Get(valuationDate, asAt);
        var instruments = await instrumentService.Get(valuationDate, asAt);
        var transactionEvents = await eventRepository.LoadStreamAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId);
        return new HoldingPositions(valuationDate, asAt, holdings, accounts, instruments, transactionEvents, filter, holdingDateBasis);
    }

    /// <summary>
    /// The "current" (no fixed asAt, default filter) path used by Get(valuationDate, holdingDateBasis) - the
    /// only shape the warm loop persists snapshots for, so the only one that attempts a seeded rebuild.
    /// </summary>
    private async Task<HoldingPositions> BuildCurrent(EventDateTime valuationDate, AuditDateTime asAt, HoldingDateBasis holdingDateBasis)
    {
        var snapshot = await snapshotRepository.FindLatestAsync(AggregateKind, Constants.Initialisation.TransactionsStreamId, valuationDate.Value, holdingDateBasis.ToString());
        if (snapshot is null)
            return await Build(valuationDate, asAt, HoldingPositionFilter.Default, holdingDateBasis);

        var holdings = await holdingService.Get(valuationDate, asAt);
        var accounts = await accountService.Get(valuationDate, asAt);
        var instruments = await instrumentService.Get(valuationDate, asAt);
        var deltaEvents = await eventRepository.LoadStreamAfterAsync<ITransactionEvent>(Constants.Initialisation.TransactionsStreamId, new EventID(snapshot.LastEventID));
        var baselineTotals = DeserializeBaseline(snapshot.PayloadJson);

        return new HoldingPositions(
            valuationDate,
            asAt,
            holdingDateBasis,
            holdings,
            accounts,
            instruments,
            HoldingPositionFilter.Default,
            baselineTotals,
            new EventID(snapshot.LastEventID),
            new AuditDateTime(snapshot.LastAuditDateTime),
            deltaEvents);
    }

    private static Dictionary<Guid, HoldingPositions.HoldingPositionTotals> DeserializeBaseline(string payloadJson)
    {
        var items = JsonSerializer.Deserialize<List<HoldingPositionSnapshotItem>>(payloadJson) ?? [];
        return items.ToDictionary(
            item => item.HoldingID,
            item => new HoldingPositions.HoldingPositionTotals(
                item.Quantity,
                item.BookCost,
                item.LastEventID.HasValue ? new EventID(item.LastEventID.Value) : null,
                item.LastAuditDateTime.HasValue ? new AuditDateTime(item.LastAuditDateTime.Value) : null));
    }

    private int InvalidateFrom(DateTime eventDateTime)
    {
        int removedCount;
        lock (cacheLock)
        {
            removedCount = 0;
            foreach (var cacheKey in cache.Keys.Where(cacheKey => cacheKey.ValuationDateTime >= eventDateTime).ToList())
            {
                if (cache.Remove(cacheKey))
                    removedCount++;
            }
        }

        // Retired synchronously (not fire-and-forget), outside the lock so a slow Marten round-trip doesn't
        // block other cache reads, but still completed before this call returns - a cache-miss read
        // immediately after invalidation must never be able to pick up a stale snapshot. GetAwaiter().GetResult()
        // is deliberate here rather than making the whole IAggregateCacheInvalidator chain async, which would
        // ripple across all ~19 services and every registration in ServiceCollectionExtensions; safe under
        // ASP.NET Core's default (no captured SynchronizationContext) execution model.
        foreach (HoldingDateBasis basis in Enum.GetValues<HoldingDateBasis>())
            snapshotRepository.RetireFromAsync(AggregateKind, Constants.Initialisation.TransactionsStreamId, eventDateTime, basis.ToString()).GetAwaiter().GetResult();

        return removedCount;
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
