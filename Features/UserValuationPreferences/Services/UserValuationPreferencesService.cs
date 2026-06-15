using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class UserValuationPreferencesService(IEventRepository eventRepository)
{
    private readonly Lock cacheLock = new();
    private readonly Dictionary<UserValuationPreferencesCacheKey, UserValuationPreferences> cache = [];

    public UserValuationPreferencesServiceDiagnostics GetDiagnostics()
    {
        lock (cacheLock)
        {
            return new UserValuationPreferencesServiceDiagnostics(cache.Count, CacheMemoryEstimator.EstimateBytes(cache.Values));
        }
    }

    public int Invalidate(IUserValuationPreferencesEvent @event) => InvalidateFrom(@event.UserID, @event.EventDateTime);

    public int InvalidateAll()
    {
        lock (cacheLock)
        {
            var removedCount = cache.Count;
            cache.Clear();
            return removedCount;
        }
    }

    public async Task<UserValuationPreferences> Get(UserID userID, EventDateTime valuationDate)
    {
        if (userID is null)
            throw new ArgumentNullException(nameof(userID));
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var cacheKey = UserValuationPreferencesCacheKey.ForAllAuditHistory(userID, valuationDate);
        var lastEventID = await eventRepository.GetLastEventIDAsync(Constants.Initialisation.UserValuationPreferencesStreamId, valuationDate.Value);

        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached) && cached.LastEventID == (lastEventID ?? Constants.Initialisation.EmptyViewEventID))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IUserValuationPreferencesEvent>(Constants.Initialisation.UserValuationPreferencesStreamId);
        var current = new UserValuationPreferences(userID, valuationDate, GetLatestAuditDateTime(valuationDate, events.ToList()), events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    public async Task<UserValuationPreferences> Get(UserID userID, EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (userID is null)
            throw new ArgumentNullException(nameof(userID));
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));
        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var cacheKey = UserValuationPreferencesCacheKey.ForAsAt(userID, valuationDate, asAt);
        lock (cacheLock)
        {
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        var events = await eventRepository.LoadStreamAsync<IUserValuationPreferencesEvent>(Constants.Initialisation.UserValuationPreferencesStreamId);
        var current = new UserValuationPreferences(userID, valuationDate, asAt, events.ToList());

        lock (cacheLock)
        {
            cache[cacheKey] = current;
            return current;
        }
    }

    private readonly record struct UserValuationPreferencesCacheKey(Guid UserID, DateTime ValuationDateTime, DateTime? AsAtDateTime)
    {
        public static UserValuationPreferencesCacheKey ForAllAuditHistory(UserID userID, EventDateTime valuationDate) => new(userID.Value, valuationDate.Value, null);
        public static UserValuationPreferencesCacheKey ForAsAt(UserID userID, EventDateTime valuationDate, AuditDateTime asAt) => new(userID.Value, valuationDate.Value, asAt.Value);
    }

    private int InvalidateFrom(UserID userID, EventDateTime eventDateTime)
    {
        lock (cacheLock)
        {
            var removedCount = 0;
            foreach (var cacheKey in cache.Keys.Where(cacheKey => cacheKey.UserID == userID.Value && cacheKey.ValuationDateTime >= eventDateTime.Value).ToList())
            {
                if (cache.Remove(cacheKey))
                    removedCount++;
            }

            return removedCount;
        }
    }

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IUserValuationPreferencesEvent> items)
    {
        var includedItems = items.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        return includedItems.Any()
            ? new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value))
            : AuditDateTimeBuilder.Create();
    }
}
