using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class AccountService(IEventRepository eventRepository) : IReferenceDataService<Accounts, AccountServiceDiagnostics>
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<AccountCacheKey, Accounts> cache = [];

    public Task<Accounts> Current => Get(ReferenceDataCurrent.EndOfToday());

    public AccountServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var accountCount = cache.Values.OrderByDescending(accounts => accounts.LastAuditDateTime.Value).FirstOrDefault()?.Items.Count ?? 0;
            return new AccountServiceDiagnostics(cache.Count, accountCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(AccountCreatedEvent @event) => InvalidateFrom(@event.EventDateTime);
    public int Invalidate(AccountModifiedEvent @event) => InvalidateFrom(@event.EventDateTime);
    public int Invalidate(AccountActiveSetEvent @event) => InvalidateFrom(@event.EventDateTime);
    public int Invalidate(AccountDisplayOrderSetEvent @event) => InvalidateFrom(@event.EventDateTime);
    public int Invalidate(AccountIdentifierSetEvent @event) => InvalidateFrom(@event.EventDateTime);
    public int Invalidate(AccountIdentifierUnsetEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        var cacheKey = AccountCacheKey.ForAllAuditHistory(valuationDate);
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

    public async Task<Accounts> Get(EventDateTime valuationDate)
    {
        var cacheKey = AccountCacheKey.ForAllAuditHistory(valuationDate);
        var lastEventID = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.AccountsStreamId, valuationDate.Value);
        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached) && cached.LastEventID == (lastEventID ?? Constants.Initialisation.EmptyViewEventID))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IAccountEvent>(Constants.Initialisation.AccountsStreamId);
        var current = new Accounts(valuationDate, events.ToList());
        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<Accounts> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        var cacheKey = AccountCacheKey.ForAsAt(valuationDate, asAt);
        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IAccountEvent>(Constants.Initialisation.AccountsStreamId);
        var current = new Accounts(valuationDate, asAt, events.ToList());
        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct AccountCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static AccountCacheKey ForAllAuditHistory(EventDateTime valuationDate) => new(valuationDate.Value, null);
        public static AccountCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) => new(valuationDate.Value, asAt.Value);
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
