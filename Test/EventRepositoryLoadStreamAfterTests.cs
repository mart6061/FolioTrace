using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;

namespace Test;

public sealed class EventRepositoryLoadStreamAfterTests
{
    [Fact]
    public async Task DefaultImplementation_ReturnsOnlyEventsStrictlyAfterTheBoundary()
    {
        var first = CreatedAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), "11111111-1111-1111-1111-111111111111");
        var second = CreatedAt(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), "22222222-2222-2222-2222-222222222222");
        var third = CreatedAt(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333");
        IEventRepository repository = new FakeEventRepository(first, second, third);

        var afterFirst = await repository.LoadStreamAfterAsync<ICountryEvent>(Guid.NewGuid(), first.EventID);

        Assert.Equal(2, afterFirst.Count);
        Assert.Contains(afterFirst, @event => @event.EventID == second.EventID);
        Assert.Contains(afterFirst, @event => @event.EventID == third.EventID);
    }

    [Fact]
    public async Task DefaultImplementation_ReturnsEmptyAfterTheLastEvent()
    {
        var first = CreatedAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), "11111111-1111-1111-1111-111111111111");
        IEventRepository repository = new FakeEventRepository(first);

        var afterFirst = await repository.LoadStreamAfterAsync<ICountryEvent>(Guid.NewGuid(), first.EventID);

        Assert.Empty(afterFirst);
    }

    [Fact]
    public async Task DefaultImplementation_ReturnsEverythingWhenTheBoundaryDoesNotExist()
    {
        var first = CreatedAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), "11111111-1111-1111-1111-111111111111");
        IEventRepository repository = new FakeEventRepository(first);

        var result = await repository.LoadStreamAfterAsync<ICountryEvent>(Guid.NewGuid(), new EventID(Guid.NewGuid()));

        Assert.Single(result);
    }

    private static CountryCreatedEvent CreatedAt(DateTime eventDateTime, string eventId) =>
        CountryCreatedEventBuilder.CreateSeed(
            Guid.Parse(eventId),
            Constants.Initialisation.UserID,
            new EventDateTime(eventDateTime),
            new AuditDateTime(eventDateTime),
            "Setup",
            "AA",
            "AAA",
            1,
            "Alpha").Value!;

    private sealed class FakeEventRepository(params IAuditEventBase[] events) : IEventRepository
    {
        public EventRepositoryCacheDiagnostics GetCacheDiagnostics() => new(true, 1, events.Length, 0, 0, []);

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IAuditEventBase =>
            Task.FromResult(events.OfType<T>().SingleOrDefault(@event => @event.EventID == eventId));

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventID?>(events.LastOrDefault()?.EventID);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventID?>(events.LastOrDefault()?.EventID);

        public Task<IReadOnlyList<IAuditEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IAuditEventBase>>(events);

        public Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
            where TEvent : class, IAuditEventBase =>
            Task.FromResult<IReadOnlyList<TEvent>>(events.OfType<TEvent>().ToList());

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
