using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;
using System.Text.Json;

namespace Services;

public sealed class TicketService(IEventRepository eventRepository, int cacheCapacity = 500, IAggregateSnapshotRepository? snapshotRepository = null)
{
    private const string AggregateKind = "Tickets";
    private readonly Lock cacheLock = new();
    private readonly BoundedLruCache<TicketCacheKey, Tickets> cache = new(cacheCapacity);

    public TicketServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var ticketCount = cache.Values
                .OrderByDescending(tickets => tickets.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new TicketServiceDiagnostics(cache.Count, ticketCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(ITicket @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = TicketCacheKey.ForAllAuditHistory(valuationDate);
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

    public async Task<Tickets> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = TicketCacheKey.ForAllAuditHistory(valuationDate);

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

    public async Task PersistSnapshotAsync(Tickets current, CancellationToken cancellationToken = default)
    {
        if (snapshotRepository is null)
            return;

        await snapshotRepository.SaveAsync(new AggregateSnapshot
        {
            Id = Guid.CreateGuid7(), AggregateKind = AggregateKind, StreamId = Constants.Initialisation.TicketsStreamId,
            ValuationDateTime = current.ValuationDateTime.Value, AsOfDateTime = current.AsOfDateTime.Value,
            LastEventID = current.LastEventID.Value, LastAuditDateTime = current.LastAuditDateTime.Value,
            PayloadJson = JsonSerializer.Serialize(current.Items), CreatedAtUtc = DateTime.UtcNow,
            SourceEventCount = current.Items.Count, Superseded = false
        }, cancellationToken);
    }

    private async Task<Tickets> BuildCurrent(EventDateTime valuationDate)
    {
        var snapshot = snapshotRepository is null ? null : await snapshotRepository.FindLatestAsync(AggregateKind, Constants.Initialisation.TicketsStreamId, valuationDate.Value);
        if (snapshot is null)
        {
            var events = await eventRepository.LoadStreamAsync<ITicket>(Constants.Initialisation.TicketsStreamId);
            return new Tickets(valuationDate, events.ToList());
        }

        var delta = (await eventRepository.LoadStreamAfterAsync<ITicket>(Constants.Initialisation.TicketsStreamId, new EventID(snapshot.LastEventID)))
            .Where(@event => @event.EventDateTime.Value <= valuationDate.Value).ToList();
        var asOf = delta.Count == 0 ? snapshot.AsOfDateTime : delta.Max(@event => @event.AuditDateTime.Value);
        return new Tickets(valuationDate, new AuditDateTime(asOf), new EventID(snapshot.LastEventID), new LastAuditDateTime(snapshot.LastAuditDateTime),
            JsonSerializer.Deserialize<List<Ticket>>(snapshot.PayloadJson) ?? [], delta);
    }

    public async Task<Tickets> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));
        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = TicketCacheKey.ForAsAt(valuationDate, asAt);
        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<ITicket>(Constants.Initialisation.TicketsStreamId);
        var current = new Tickets(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct TicketCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static TicketCacheKey ForAllAuditHistory(EventDateTime valuationDate) => new(valuationDate.Value, null);
        public static TicketCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) => new(valuationDate.Value, asAt.Value);
    }

    private int InvalidateFrom(EventDateTime eventDateTime)
    {
        snapshotRepository?.RetireFromAsync(AggregateKind, Constants.Initialisation.TicketsStreamId, eventDateTime.Value).GetAwaiter().GetResult();
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
