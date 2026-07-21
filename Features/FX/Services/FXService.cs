using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Snapshots;
using FolioTrace.Types;
using Repository;
using System.Text.Json;

namespace Services;

public sealed class FXService(IEventRepository eventRepository, int cacheCapacity = 500, IAggregateSnapshotRepository? snapshotRepository = null) : IReferenceDataService<FXs, FXServiceDiagnostics>
{
    private const string AggregateKind = "FXs";
    private readonly Lock cacheLock = new();
    private readonly BoundedLruCache<FXCacheKey, FXs> cache = new(cacheCapacity);

    public Task<FXs> Current => Get(ReferenceDataCurrent.EndOfToday());

    public FXServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var fxCount = cache.Values
                .OrderByDescending(fxs => fxs.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new FXServiceDiagnostics(cache.Count, fxCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(FXCreatedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public int Invalidate(FXActiveModifiedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = FXCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task<FXs> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = FXCacheKey.ForAllAuditHistory(valuationDate);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var current = await BuildCurrent(valuationDate);

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task PersistSnapshotAsync(FXs current, CancellationToken cancellationToken = default)
    {
        if (snapshotRepository is null)
            return;

        await snapshotRepository.SaveAsync(new AggregateSnapshot
        {
            Id = Guid.CreateGuid7(), AggregateKind = AggregateKind, StreamId = Constants.Initialisation.FXsStreamId,
            ValuationDateTime = current.ValuationDateTime.Value, AsOfDateTime = current.AsOfDateTime.Value,
            LastEventID = current.LastEventID.Value, LastAuditDateTime = current.LastAuditDateTime.Value,
            PayloadJson = JsonSerializer.Serialize(current.Items), CreatedAtUtc = DateTime.UtcNow,
            SourceEventCount = current.Items.Count, Superseded = false
        }, cancellationToken);
    }

    private async Task<FXs> BuildCurrent(EventDateTime valuationDate)
    {
        var snapshot = snapshotRepository is null ? null : await snapshotRepository.FindLatestAsync(AggregateKind, Constants.Initialisation.FXsStreamId, valuationDate.Value);
        if (snapshot is null)
        {
            var events = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId);
            return new FXs(valuationDate, events.ToList());
        }

        var delta = (await eventRepository.LoadStreamAfterAsync<IFXEvent>(Constants.Initialisation.FXsStreamId, new EventID(snapshot.LastEventID)))
            .Where(@event => @event.EventDateTime.Value <= valuationDate.Value).ToList();
        var asOf = delta.Count == 0 ? snapshot.AsOfDateTime : delta.Max(@event => @event.AuditDateTime.Value);
        return new FXs(valuationDate, new AuditDateTime(asOf), new EventID(snapshot.LastEventID), new LastAuditDateTime(snapshot.LastAuditDateTime),
            JsonSerializer.Deserialize<List<FX>>(snapshot.PayloadJson) ?? [], delta);
    }

    public async Task<FXs> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = FXCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId);
        var current = new FXs(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct FXCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static FXCacheKey ForAllAuditHistory(EventDateTime valuationDate) =>
            new(valuationDate.Value, null);

        public static FXCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) =>
            new(valuationDate.Value, asAt.Value);
    }

    private int InvalidateFrom(EventDateTime eventDateTime)
    {
        snapshotRepository?.RetireFromAsync(AggregateKind, Constants.Initialisation.FXsStreamId, eventDateTime.Value).GetAwaiter().GetResult();
        lock (cacheLock)
        {
            var removedCount = 0;

            foreach (var cacheKey in cache.Keys.Where(cacheKey => !cacheKey.AsAtDateTime.HasValue && cacheKey.ValuationDateTime >= eventDateTime.Value).ToList())
            {
                if (cache.Remove(cacheKey))
                    removedCount++;
            }

            return removedCount;
        }
    }
}
