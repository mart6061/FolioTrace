using System.Text.Json;
using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace Test;

public sealed class UserBookmarksTests
{
    private static readonly UserID UserID = new(Guid.Parse("9461ecf2-9632-4088-af90-a08875a5223d"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 1, DateTimeKind.Utc));

    [Fact]
    public void CreatedBuilder_AcceptsPathBookmark()
    {
        var bookmarkID = Guid.CreateGuid7();
        var result = UserBookmarkCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Create bookmark",
            bookmarkID,
            UserBookmarkType.Base,
            "/Data/Reference/Accounts",
            1);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Value);
        Assert.Equal(bookmarkID, result.Value!.BookmarkID);
        Assert.Equal(UserBookmarkType.Base, result.Value.BookmarkType);
    }

    [Fact]
    public void RequestJson_AcceptsStringBookmarkType()
    {
        var json = """
            {
              "UserID": "334f6bb3-762d-4d10-9752-f913d75f7c6c",
              "EventDateTime": "2026-05-30T17:28:31.000Z",
              "Reason": "Create Base bookmark",
              "BookmarkID": "e8486990-2da8-41d5-971b-c46aed2f3128",
              "BookmarkType": "Base",
              "Url": "/User/Preferences",
              "DisplayOrder": 1
            }
            """;

        var request = JsonSerializer.Deserialize<UserBookmarkRequest>(json);

        Assert.NotNull(request);
        Assert.Equal(UserBookmarkType.Base, request!.BookmarkType);
        Assert.Equal("/User/Preferences", request.Url);
        Assert.Equal(1, request.DisplayOrder.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Data/Reference/Accounts")]
    [InlineData("/Data/Reference/Accounts?auditDateTime=2026-01-01")]
    public void CreatedBuilder_RejectsInvalidUrls(string url)
    {
        var result = UserBookmarkCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Create bookmark",
            Guid.CreateGuid7(),
            UserBookmarkType.Base,
            url,
            1);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void DisplayOrderBuilder_RejectsNonPositiveOrder()
    {
        var result = UserBookmarkDisplayOrderSetEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Set bookmark order",
            Guid.CreateGuid7(),
            0);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void DeletedBuilder_RejectsEmptyBookmarkID()
    {
        var result = UserBookmarkDeletedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Delete bookmark",
            Guid.Empty);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Service_RebuildsBookmarksAndHonorsDisplayOrder()
    {
        var firstID = Guid.CreateGuid7();
        var secondID = Guid.CreateGuid7();
        var firstAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 1, DateTimeKind.Utc));
        var secondAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 2, DateTimeKind.Utc));
        var thirdAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 3, DateTimeKind.Utc));
        var first = UserBookmarkCreatedEventBuilder.CreateSeed(Guid.CreateGuid7(), UserID, EventDate, firstAudit, "Create bookmark", firstID, UserBookmarkType.Base, "/Data/Reference/Accounts", 1).Value!;
        var second = UserBookmarkCreatedEventBuilder.CreateSeed(Guid.CreateGuid7(), UserID, EventDate, secondAudit, "Create bookmark", secondID, UserBookmarkType.Query, "/Blotter", 2).Value!;
        var reorder = UserBookmarkDisplayOrderSetEventBuilder.CreateSeed(Guid.CreateGuid7(), UserID, EventDate, thirdAudit, "Set bookmark order", secondID, 1).Value!;
        var service = new UserBookmarksService(new FakeEventRepository(first, second, reorder));

        var beforeReorder = await service.Get(UserID, EventDate, secondAudit);
        var afterReorder = await service.Get(UserID, EventDate, thirdAudit);

        Assert.Equal(firstID, beforeReorder.Items[0].BookmarkID);
        Assert.Equal(secondID, afterReorder.Items[0].BookmarkID);
        Assert.Equal(Constants.Initialisation.EmptyViewEventID, (await new UserBookmarksService(new FakeEventRepository()).Get(UserID, EventDate, AuditDate)).LastEventID);
    }

    [Fact]
    public async Task Service_RemovesDeletedBookmarks()
    {
        var bookmarkID = Guid.CreateGuid7();
        var firstAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 1, DateTimeKind.Utc));
        var secondAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 2, DateTimeKind.Utc));
        var created = UserBookmarkCreatedEventBuilder.CreateSeed(Guid.CreateGuid7(), UserID, EventDate, firstAudit, "Create bookmark", bookmarkID, UserBookmarkType.Base, "/Data/Reference/Accounts", 1).Value!;
        var deleted = UserBookmarkDeletedEventBuilder.CreateSeed(Guid.CreateGuid7(), UserID, EventDate, secondAudit, "Delete bookmark", bookmarkID).Value!;
        var service = new UserBookmarksService(new FakeEventRepository(created, deleted));

        var beforeDelete = await service.Get(UserID, EventDate, firstAudit);
        var afterDelete = await service.Get(UserID, EventDate, secondAudit);

        Assert.Single(beforeDelete.Items);
        Assert.Empty(afterDelete.Items);
    }

    private sealed class FakeEventRepository(params IEventBase[] events) : IEventRepository
    {
        private readonly List<IAuditEventBase> events = [.. events];

        public EventRepositoryCacheDiagnostics GetCacheDiagnostics() => new(true, 1, this.events.Count, 0, 0, []);

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            events.Clear();
            return Task.CompletedTask;
        }

        public Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IAuditEventBase =>
            Task.FromResult(events.SingleOrDefault(@event => @event.EventID == eventId) as T);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult(events.LastOrDefault()?.EventID);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default)
        {
            var latest = events
                .OfType<IEventBase>().Where(@event => @event.EventDateTime.Value <= valuationDateTime && (!asOfDateTime.HasValue || @event.AuditDateTime.Value <= asOfDateTime.Value))
                .OrderBy(@event => @event.EventDateTime.Value)
                .ThenBy(@event => @event.AuditDateTime.Value)
                .ThenBy(@event => @event.EventID.Value)
                .LastOrDefault();

            return Task.FromResult(latest?.EventID);
        }

        public Task<IReadOnlyList<IAuditEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<IAuditEventBase>)events.ToList());

        public Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
            where TEvent : class, IAuditEventBase =>
            Task.FromResult((IReadOnlyList<TEvent>)events.OfType<TEvent>().ToList());

        public Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
            where TAggregate : class
            where TEvent : class, IAuditEventBase
        {
            this.events.Clear();
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
    }
}
