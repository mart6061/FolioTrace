using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Snapshots;
using FolioTrace.Types;
using Repository;
using System.Text.Json;

namespace Services;

public sealed class FXRateService(IEventRepository eventRepository, int cacheCapacity = 500, IAggregateSnapshotRepository? snapshotRepository = null, FXService? fxService = null)
{
    private const string AggregateKind = "FXRates";
    private readonly Lock cacheLock = new();
    private readonly BoundedLruCache<FXRateCacheKey, FXRates> cache = new(cacheCapacity);

    public FXRateServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var rateCount = cache.Values
                .OrderByDescending(rates => rates.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new FXRateServiceDiagnostics(cache.Count, rateCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(FXCreatedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public int Invalidate(FXActiveModifiedEvent @event) => InvalidateFrom(@event.EventDateTime);

    public int Invalidate(FXRateSetEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = FXRateCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task<FXRates> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = FXRateCacheKey.ForAllAuditHistory(valuationDate);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var current = await TryBuildFromSnapshotAsync(valuationDate);
        if (current is null)
        {
            var fxEvents = await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId);
            var rateEvents = await eventRepository.LoadStreamAsync<IFXRateEvent>(Constants.Initialisation.FXRatesStreamId);
            current = new FXRates(valuationDate, fxEvents.ToList(), rateEvents.ToList());
        }

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task PersistSnapshotAsync(FXRates current, CancellationToken cancellationToken = default)
    {
        if (snapshotRepository is null)
            return;

        var checkpoint = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.FXRatesStreamId, current.ValuationDateTime.Value, cancellationToken: cancellationToken)
            ?? Constants.Initialisation.EmptyViewEventID;
        await snapshotRepository.SaveAsync(new AggregateSnapshot
        {
            Id = Guid.CreateGuid7(), AggregateKind = AggregateKind, StreamId = Constants.Initialisation.FXRatesStreamId,
            ValuationDateTime = current.ValuationDateTime.Value, AsOfDateTime = current.AsOfDateTime.Value,
            LastEventID = checkpoint.Value, LastAuditDateTime = current.LastAuditDateTime.Value,
            PayloadJson = JsonSerializer.Serialize(current.Items), CreatedAtUtc = DateTime.UtcNow,
            SourceEventCount = current.Items.Count, Superseded = false
        }, cancellationToken);
    }

    private async Task<FXRates?> TryBuildFromSnapshotAsync(EventDateTime valuationDate)
    {
        if (snapshotRepository is null)
            return null;

        var snapshot = await snapshotRepository.FindLatestAsync(AggregateKind, Constants.Initialisation.FXRatesStreamId, valuationDate.Value);
        if (snapshot is null)
            return null;

        var fxs = fxService is null
            ? new FXs(valuationDate, (await eventRepository.LoadStreamAsync<IFXEvent>(Constants.Initialisation.FXsStreamId)).ToList())
            : await fxService.Get(valuationDate);
        var delta = (await eventRepository.LoadStreamAfterAsync<IFXRateEvent>(Constants.Initialisation.FXRatesStreamId, new EventID(snapshot.LastEventID)))
            .Where(@event => @event.EventDateTime.Value <= valuationDate.Value).ToList();
        var asOf = new[] { snapshot.AsOfDateTime, fxs.LastAuditDateTime.Value, delta.Select(@event => @event.AuditDateTime.Value).DefaultIfEmpty(snapshot.AsOfDateTime).Max() }.Max();
        var current = new FXRates(valuationDate, new AuditDateTime(asOf), new EventID(snapshot.LastEventID), new LastAuditDateTime(snapshot.LastAuditDateTime),
            JsonSerializer.Deserialize<List<FXRate>>(snapshot.PayloadJson) ?? []);
        foreach (var @event in delta)
            current.Apply(@event, fxs);
        current.Apply(fxs);
        return current;
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
        snapshotRepository?.RetireFromAsync(AggregateKind, Constants.Initialisation.FXRatesStreamId, eventDateTime.Value).GetAwaiter().GetResult();
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
