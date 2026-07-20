using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class TicketService(IEventRepository eventRepository, int cacheCapacity = 500)
{
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
        var lastEventID = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.TicketsStreamId, valuationDate.Value);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached) && cached.LastEventID == (lastEventID ?? Constants.Initialisation.EmptyViewEventID))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<ITicket>(Constants.Initialisation.TicketsStreamId);
        var current = new Tickets(valuationDate, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
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
