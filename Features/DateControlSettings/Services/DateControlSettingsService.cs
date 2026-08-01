using FolioTrace; using FolioTrace.Aggregates; using FolioTrace.Types; using Repository;
namespace Services;
public sealed class DateControlSettingsService(IEventRepository eventRepository, int cacheCapacity = 500)
{
    private readonly Lock cacheLock = new(); private readonly BoundedLruCache<(DateTime Event, DateTime? Audit), DateControlSettings> cache = new(cacheCapacity);
    public DateControlSettingsServiceDiagnostics GetDiagnostics() { lock (cacheLock) return new(cache.Count, CacheMemoryEstimator.EstimateBytes(cache.Values)); }
    public int Invalidate(IDateControlSettingsEvent @event) { lock (cacheLock) { var count = cache.Count; cache.Clear(); return count; } }
    public int InvalidateAll() { lock (cacheLock) { var count = cache.Count; cache.Clear(); return count; } }
    public Task<DateControlSettings> Get(EventDateTime eventDateTime) => Get(eventDateTime, null);
    public async Task<DateControlSettings> Get(EventDateTime eventDateTime, AuditDateTime? auditDateTime) { var key = (eventDateTime.Value, auditDateTime?.Value); lock (cacheLock) if (cache.TryGetValue(key, out var found)) return found; var events = (await eventRepository.LoadStreamAsync<IDateControlSettingsEvent>(Constants.Initialisation.DateControlSettingsStreamId)).ToList(); var asOf = auditDateTime ?? (events.Count > 0 ? new AuditDateTime(events.Max(x => x.AuditDateTime.Value)) : AuditDateTimeBuilder.Create()); var current = new DateControlSettings(eventDateTime, asOf, events); lock (cacheLock) cache[key] = current; return current; }
}
