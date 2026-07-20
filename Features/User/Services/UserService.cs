using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class UserService(IEventRepository eventRepository, int cacheCapacity = 500)
{
    private readonly Lock cacheLock = new();
    private readonly BoundedLruCache<UserCacheKey, Users> cache = new(cacheCapacity);
    private readonly Dictionary<UserID, CurrentUserCacheEntry> currentUserCache = [];

    public UserServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            var userCount = cache.Values
                .OrderByDescending(users => users.LastAuditDateTime.Value)
                .FirstOrDefault()
                ?.Items.Count ?? 0;

            return new UserServiceDiagnostics(cache.Count, userCount, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(IUserEvent @event) => InvalidateFrom(@event.EventDateTime);

    public bool IsCached(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = UserCacheKey.ForAllAuditHistory(valuationDate);

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
            currentUserCache.Clear();
            return removedCount;
        }
    }

    public async Task<User?> FindCurrentAsync(UserID userID, CancellationToken cancellationToken = default)
    {
        if (userID is null)
            throw new ArgumentNullException(nameof(userID));

        var lastEventID = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.UsersStreamId, cancellationToken)
            ?? Constants.Initialisation.EmptyViewEventID;

        lock (cacheLock)
        {
            if (currentUserCache.TryGetValue(userID, out var cached) && cached.UsersStreamLastEventID == lastEventID)
                return cached.User;
        }

        var events = await eventRepository.LoadStreamAsync<IUserEvent>(Constants.Initialisation.UsersStreamId, cancellationToken);
        var user = new Users(ReferenceDataCurrent.EndOfToday(), events.ToList()).Find(userID);

        lock (cacheLock)
        {
            currentUserCache[userID] = new CurrentUserCacheEntry(lastEventID, user);
            return user;
        }
    }

    public async Task<Users> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = UserCacheKey.ForAllAuditHistory(valuationDate);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IUserEvent>(Constants.Initialisation.UsersStreamId);
        var current = new Users(valuationDate, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<Users> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));
        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = UserCacheKey.ForAsAt(valuationDate, asAt);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IUserEvent>(Constants.Initialisation.UsersStreamId);
        var current = new Users(valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct UserCacheKey(DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static UserCacheKey ForAllAuditHistory(EventDateTime valuationDate) => new(valuationDate.Value, null);
        public static UserCacheKey ForAsAt(EventDateTime valuationDate, AuditDateTime asAt) => new(valuationDate.Value, asAt.Value);
    }

    private sealed record CurrentUserCacheEntry(EventID UsersStreamLastEventID, User? User);

    private int InvalidateFrom(EventDateTime eventDateTime)
    {
        lock (cacheLock)
        {
            var removedCount = 0;
            foreach (var cacheKey in cache.Keys.Where(cacheKey => !cacheKey.AsAtDateTime.HasValue && cacheKey.ValuationDateTime >= eventDateTime.Value).ToList())
            {
                if (cache.Remove(cacheKey))
                    removedCount++;
            }

            currentUserCache.Clear();

            return removedCount;
        }
    }
}
