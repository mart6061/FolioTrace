using FolioTrace; using FolioTrace.Aggregates; using FolioTrace.Types; using Repository;
namespace Services;
public sealed class UserDateControlSettingsService(IEventRepository eventRepository, int cacheCapacity = 500)
{
    private readonly Lock cacheLock = new(); private readonly BoundedLruCache<(Guid User, DateTime Event, DateTime? Audit), UserDateControlSettings> cache = new(cacheCapacity);
    public DateControlSettingsServiceDiagnostics GetDiagnostics() { lock (cacheLock) return new(cache.Count, CacheMemoryEstimator.EstimateBytes(cache.Values)); }
    public int Invalidate(IUserDateControlSettingsEvent @event) { lock (cacheLock) { var count = cache.Count; cache.Clear(); return count; } }
    public int InvalidateAll() { lock (cacheLock) { var count = cache.Count; cache.Clear(); return count; } }
    public Task<UserDateControlSettings> Get(UserID userID, EventDateTime eventDateTime) => Get(userID, eventDateTime, null);
    public async Task<UserDateControlSettings> Get(UserID userID, EventDateTime eventDateTime, AuditDateTime? auditDateTime) { var key = (userID.Value, eventDateTime.Value, auditDateTime?.Value); lock (cacheLock) if (cache.TryGetValue(key, out var found)) return found; var events = (await eventRepository.LoadStreamAsync<IUserDateControlSettingsEvent>(Constants.Initialisation.UserDateControlSettingsStreamId)).ToList(); var asOf = auditDateTime ?? (events.Count > 0 ? new AuditDateTime(events.Max(x => x.AuditDateTime.Value)) : AuditDateTimeBuilder.Create()); var current = new UserDateControlSettings(userID, eventDateTime, asOf, events); lock (cacheLock) cache[key] = current; return current; }
}
