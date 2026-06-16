using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace Test;

public sealed class UserServiceTests
{
    private static readonly UserID UserID = new(Guid.Parse("a283b037-65fc-44c6-8da4-996c7ab143db"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime FirstAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 1, DateTimeKind.Utc));
    private static readonly AuditDateTime SecondAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 2, DateTimeKind.Utc));
    private static readonly AuditDateTime ThirdAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 3, DateTimeKind.Utc));

    [Fact]
    public void Users_ReplaysCreatedAndModifiedEventsAndHonorsAuditDate()
    {
        var created = CreateUserCreated("Original", EventDate, FirstAudit);
        var modified = CreateUserModified("Renamed", EventDate, SecondAudit);

        var beforeModification = new Users(EventDate, FirstAudit, [created, modified]);
        var afterModification = new Users(EventDate, SecondAudit, [created, modified]);

        Assert.Equal("Original", Assert.Single(beforeModification.Items).DisplayName);
        Assert.Equal("Renamed", Assert.Single(afterModification.Items).DisplayName);
        Assert.Equal(modified.EventID, afterModification.LastEventID);
        Assert.Same(afterModification.Items.Single(), afterModification.Find(UserID));
    }

    [Fact]
    public void Users_ReplaysSignedInAndSignedOutEventsAndHonorsAuditDate()
    {
        var signedInDate = EventDateTimeBuilder.Create(new DateTime(2025, 5, 1, 8, 0, 0, DateTimeKind.Utc));
        var signedOutDate = EventDateTimeBuilder.Create(new DateTime(2025, 5, 1, 18, 0, 0, DateTimeKind.Utc));
        var created = CreateUserCreated("Original", EventDate, FirstAudit);
        var signedIn = CreateUserSignedIn(signedInDate, SecondAudit);
        var signedOut = CreateUserSignedOut(signedOutDate, ThirdAudit);

        var beforeSignOut = new Users(signedOutDate, SecondAudit, [created, signedIn, signedOut]);
        var afterSignOut = new Users(signedOutDate, ThirdAudit, [created, signedIn, signedOut]);

        var beforeUser = Assert.Single(beforeSignOut.Items);
        Assert.Equal(signedInDate, beforeUser.LastSignedIn);
        Assert.Null(beforeUser.LastSignedOut);

        var afterUser = Assert.Single(afterSignOut.Items);
        Assert.Equal(signedInDate, afterUser.LastSignedIn);
        Assert.Equal(signedOutDate, afterUser.LastSignedOut);
        Assert.Equal(signedOut.EventID, afterSignOut.LastEventID);
    }

    [Fact]
    public async Task Service_ReturnsEmptyAggregateWithoutEvents()
    {
        var service = new UserService(new FakeEventRepository());

        var users = await service.Get(EventDate, FirstAudit);

        Assert.Empty(users.Items);
        Assert.Equal(Constants.Initialisation.EmptyViewEventID, users.LastEventID);
    }

    [Fact]
    public async Task Service_CachesDiagnosticsAndInvalidatesAffectedViews()
    {
        var created = CreateUserCreated(
            "Original",
            EventDateTimeBuilder.Create(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            AuditDateTimeBuilder.Create(new DateTime(2025, 1, 1, 0, 0, 1, DateTimeKind.Utc)));
        var modified = CreateUserModified(
            "Renamed",
            EventDateTimeBuilder.Create(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
            AuditDateTimeBuilder.Create(new DateTime(2025, 6, 1, 0, 0, 1, DateTimeKind.Utc)));
        var repository = new FakeEventRepository(created);
        var service = new UserService(repository);
        var beforeModificationDate = EventDateTimeBuilder.Create(new DateTime(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc));
        var afterModificationDate = EventDateTimeBuilder.Create(new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc));

        var beforeModification = await service.Get(beforeModificationDate);
        var afterModification = await service.Get(afterModificationDate);

        Assert.Equal("Original", beforeModification.Find(UserID)?.DisplayName);
        Assert.Equal("Original", afterModification.Find(UserID)?.DisplayName);
        Assert.Equal(2, service.GetDiagnostics().CacheEntryCount);
        Assert.Equal(1, service.GetDiagnostics().UserCount);

        repository.Append(modified);
        var removedCount = service.Invalidate(modified);

        Assert.Equal(1, removedCount);
        Assert.Equal(1, service.GetDiagnostics().CacheEntryCount);

        beforeModification = await service.Get(beforeModificationDate);
        afterModification = await service.Get(afterModificationDate);

        Assert.Equal("Original", beforeModification.Find(UserID)?.DisplayName);
        Assert.Equal("Renamed", afterModification.Find(UserID)?.DisplayName);
        Assert.Equal(2, service.GetDiagnostics().CacheEntryCount);
    }

    private static UserCreatedEvent CreateUserCreated(string displayName, EventDateTime eventDateTime, AuditDateTime auditDateTime) =>
        UserCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            eventDateTime,
            auditDateTime,
            "Create user",
            displayName,
            new UserDisplayPreferences(false, true),
            new UserProfileValuationPreferences(eventDateTime, true, true)).Value!;

    private static UserModifiedEvent CreateUserModified(string displayName, EventDateTime eventDateTime, AuditDateTime auditDateTime) =>
        UserModifiedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            eventDateTime,
            auditDateTime,
            "Modify user",
            displayName,
            new UserDisplayPreferences(true, false),
            new UserProfileValuationPreferences(eventDateTime, false, true)).Value!;

    private static UserSignedInEvent CreateUserSignedIn(EventDateTime eventDateTime, AuditDateTime auditDateTime) =>
        UserSignedInEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            eventDateTime,
            auditDateTime,
            "Sign in user").Value!;

    private static UserSignedOutEvent CreateUserSignedOut(EventDateTime eventDateTime, AuditDateTime auditDateTime) =>
        UserSignedOutEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            eventDateTime,
            auditDateTime,
            "Sign out user").Value!;

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
