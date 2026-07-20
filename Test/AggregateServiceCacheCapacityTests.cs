using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace Test;

public sealed class AggregateServiceCacheCapacityTests
{
    [Fact]
    public async Task GetDiagnostics_ReflectsTheConfiguredCacheCapacity()
    {
        var repository = new FakeEventRepository();
        var service = new CountryService(repository, cacheCapacity: 2);

        await service.Get(new EventDateTime(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await service.Get(new EventDateTime(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await service.Get(new EventDateTime(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(2, service.GetDiagnostics().CacheEntryCount);
    }

    [Fact]
    public async Task Get_EvictsTheLeastRecentlyUsedValuationDateFirst()
    {
        var repository = new FakeEventRepository();
        var service = new CountryService(repository, cacheCapacity: 2);

        var first = new EventDateTime(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var second = new EventDateTime(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var third = new EventDateTime(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        await service.Get(first);
        await service.Get(second);

        Assert.True(service.IsCached(first));
        Assert.True(service.IsCached(second));

        // Re-touch "first" so "second" becomes the least-recently-used entry.
        await service.Get(first);
        await service.Get(third);

        Assert.True(service.IsCached(first));
        Assert.False(service.IsCached(second));
        Assert.True(service.IsCached(third));
    }

    private sealed class FakeEventRepository : IEventRepository
    {
        public EventRepositoryCacheDiagnostics GetCacheDiagnostics() => new(true, 0, 0, 0, 0, []);

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IAuditEventBase =>
            Task.FromResult<T?>(null);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventID?>(null);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventID?>(null);

        public Task<IReadOnlyList<IAuditEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IAuditEventBase>>([]);

        public Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
            where TEvent : class, IAuditEventBase =>
            Task.FromResult<IReadOnlyList<TEvent>>([]);

        public Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
            where TAggregate : class
            where TEvent : class, IAuditEventBase =>
            Task.CompletedTask;

        public Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IAuditEventBase =>
            Task.CompletedTask;

        public Task AppendAsync(Guid streamId, IEnumerable<IAuditEventBase> events, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
