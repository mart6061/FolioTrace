using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class UserMenuPreferencesService(IEventRepository eventRepository)
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<UserMenuPreferencesCacheKey, UserMenuPreferences> cache = [];

    public UserMenuPreferencesServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            return new UserMenuPreferencesServiceDiagnostics(cache.Count, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(IUserMenuPreferencesEvent @event) => InvalidateFrom(@event.UserID, @event.EventDateTime);

    public int InvalidateAll()
    {
        lock (cacheLock)
        {
            var removedCount = cache.Count;
            cache.Clear();
            return removedCount;
        }
    }

    public async Task<UserMenuPreferences> Get(UserID userID, EventDateTime valuationDate)
    {
        if (userID is null)
            throw new ArgumentNullException(nameof(userID));
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = UserMenuPreferencesCacheKey.ForAllAuditHistory(userID, valuationDate);
        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IUserMenuPreferencesEvent>(Constants.Initialisation.UserMenuPreferencesStreamId);
        var current = new UserMenuPreferences(userID, valuationDate, GetLatestAuditDateTime(valuationDate, events.ToList()), events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<UserMenuPreferences> Get(UserID userID, EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (userID is null)
            throw new ArgumentNullException(nameof(userID));
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));
        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = UserMenuPreferencesCacheKey.ForAsAt(userID, valuationDate, asAt);
        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IUserMenuPreferencesEvent>(Constants.Initialisation.UserMenuPreferencesStreamId);
        var current = new UserMenuPreferences(userID, valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct UserMenuPreferencesCacheKey(Guid UserID, DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static UserMenuPreferencesCacheKey ForAllAuditHistory(UserID userID, EventDateTime valuationDate) => new(userID.Value, valuationDate.Value, null);
        public static UserMenuPreferencesCacheKey ForAsAt(UserID userID, EventDateTime valuationDate, AuditDateTime asAt) => new(userID.Value, valuationDate.Value, asAt.Value);
    }

    private int InvalidateFrom(UserID userID, EventDateTime eventDateTime)
    {
        lock (cacheLock)
        {
            var removedCount = 0;
            foreach (var cacheKey in cache.Keys.Where(cacheKey => !cacheKey.AsAtDateTime.HasValue && cacheKey.UserID == userID.Value && cacheKey.ValuationDateTime >= eventDateTime.Value).ToList())
            {
                if (cache.Remove(cacheKey))
                    removedCount++;
            }

            return removedCount;
        }
    }

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IUserMenuPreferencesEvent> items)
    {
        var includedItems = items.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        return includedItems.Any()
            ? new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value))
            : AuditDateTimeBuilder.Create();
    }
}
