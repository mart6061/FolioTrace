using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace Test;

public sealed class CurrencyServiceCacheInvalidationTests
{
    [Fact]
    public async Task Invalidate_RemovesOnlyCachedViewsAffectedByEventDateTime()
    {
        var created = CurrencyCreatedEventBuilder.CreateSeed(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Constants.Initialisation.UserID,
            new EventDateTime(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            new AuditDateTime(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            "Setup",
            "AAA",
            1,
            2,
            "Alpha currency").Value!;

        var modified = CurrencyModifiedEventBuilder.CreateSeed(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Constants.Initialisation.UserID,
            new EventDateTime(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
            new AuditDateTime(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
            "Rename",
            "AAA",
            1,
            2,
            "Alpha currency renamed").Value!;

        var repository = new FakeEventRepository(created);
        var service = new CurrencyService(repository);

        var beforeModification = await service.Get(new EventDateTime(new DateTime(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc)));
        var afterModification = await service.Get(new EventDateTime(new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc)));

        Assert.Equal("Alpha currency", beforeModification.Items.Single().Name);
        Assert.Equal("Alpha currency", afterModification.Items.Single().Name);
        Assert.Equal(2, service.GetDiagnostics().CacheEntryCount);

        repository.Append(modified);

        var removedCount = service.Invalidate(modified);

        Assert.Equal(1, removedCount);
        Assert.Equal(1, service.GetDiagnostics().CacheEntryCount);

        beforeModification = await service.Get(new EventDateTime(new DateTime(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc)));
        afterModification = await service.Get(new EventDateTime(new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc)));

        Assert.Equal("Alpha currency", beforeModification.Items.Single().Name);
        Assert.Equal("Alpha currency renamed", afterModification.Items.Single().Name);
        Assert.Equal(2, service.GetDiagnostics().CacheEntryCount);
    }

    private sealed class FakeEventRepository(params IEventBase[] events) : IEventRepository
    {
        private readonly List<IAuditEventBase> events = [.. events];

        public EventRepositoryCacheDiagnostics GetCacheDiagnostics() => new(true, 1, events.Count, 0, 0, []);

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            events.Clear();
            return Task.CompletedTask;
        }

        public Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IAuditEventBase =>
            Task.FromResult(events.OfType<T>().SingleOrDefault(@event => @event.EventID == eventId));

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventID?>(events.LastOrDefault()?.EventID);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventID?>(events
                .OfType<IEventBase>().Where(@event => @event.EventDateTime.Value <= valuationDateTime && (!asOfDateTime.HasValue || @event.AuditDateTime.Value <= asOfDateTime.Value))
                .OrderBy(@event => @event.EventDateTime.Value)
                .ThenBy(@event => @event.AuditDateTime.Value)
                .ThenBy(@event => @event.EventID.Value)
                .LastOrDefault()
                ?.EventID);

        public Task<IReadOnlyList<IAuditEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IAuditEventBase>>(events);

        public Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
            where TEvent : class, IAuditEventBase =>
            Task.FromResult<IReadOnlyList<TEvent>>(events.OfType<TEvent>().ToList());

        public Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
            where TAggregate : class
            where TEvent : class, IAuditEventBase
        {
            this.events.AddRange(events);
            return Task.CompletedTask;
        }

        public Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IAuditEventBase
        {
            events.Add(@event);
            return Task.CompletedTask;
        }

        public Task AppendAsync(Guid streamId, IEnumerable<IAuditEventBase> events, CancellationToken cancellationToken = default)
        {
            this.events.AddRange(events);
            return Task.CompletedTask;
        }

        public void Append(IEventBase @event) => events.Add(@event);
    }
}
