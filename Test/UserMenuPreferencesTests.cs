using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace Test;

public sealed class UserMenuPreferencesTests
{
    private static readonly UserID UserID = new(Guid.Parse("65f2541d-255d-4f93-8d17-6c2de514f32f"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 1, DateTimeKind.Utc));

    [Fact]
    public void CreatedBuilder_AcceptsDefaultVisibleItems()
    {
        var result = UserMenuPreferencesCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Create menu preferences",
            UserMenuPreferenceDefaults.CreateVisibleItems());

        Assert.True(result.IsValid);
        Assert.NotNull(result.Value);
        Assert.All(result.Value!.Items, item => Assert.True(item.Visible));
        Assert.Equal(UserMenuPreferenceDefaults.ControlledMenuItemIDs.Count, result.Value.Items.Count);
        Assert.Contains(result.Value.Items, item => item.MenuItemID == UserMenuPreferenceDefaults.Bookmarks);
        Assert.Contains(result.Value.Items, item => item.MenuItemID == UserMenuPreferenceDefaults.Viewer);
        Assert.Contains(result.Value.Items, item => item.MenuItemID == UserMenuPreferenceDefaults.DataList);
        Assert.Contains(result.Value.Items, item => item.MenuItemID == UserMenuPreferenceDefaults.DataListFX);
        Assert.Contains(result.Value.Items, item => item.MenuItemID == UserMenuPreferenceDefaults.Diagnostics);
        Assert.Contains(result.Value.Items, item => item.MenuItemID == UserMenuPreferenceDefaults.Ideas);
    }

    [Fact]
    public void Normalize_MapsLegacyValuationsMenuIDToViewer()
    {
        var items = UserMenuPreferenceDefaults.CreateVisibleItems()
            .Where(item => item.MenuItemID != UserMenuPreferenceDefaults.Viewer)
            .Append(new UserMenuPreferenceItem("value-valuations", false))
            .ToList();

        var normalized = UserMenuPreferenceDefaults.Normalize(items);

        Assert.Contains(normalized, item => item.MenuItemID == UserMenuPreferenceDefaults.Viewer && !item.Visible);
        Assert.DoesNotContain(normalized, item => item.MenuItemID == "value-valuations");
    }

    [Fact]
    public void CreatedBuilder_AcceptsLegacyValuationsMenuID()
    {
        var items = UserMenuPreferenceDefaults.CreateVisibleItems()
            .Where(item => item.MenuItemID != UserMenuPreferenceDefaults.Viewer)
            .Append(new UserMenuPreferenceItem("value-valuations", false))
            .ToList();

        var result = UserMenuPreferencesCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Create menu preferences",
            items);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Value);
        Assert.Contains(result.Value!.Items, item => item.MenuItemID == UserMenuPreferenceDefaults.Viewer && !item.Visible);
        Assert.DoesNotContain(result.Value.Items, item => item.MenuItemID == "value-valuations");
    }

    [Theory]
    [InlineData("data", UserMenuPreferenceDefaults.DataList)]
    [InlineData("reference", UserMenuPreferenceDefaults.DataList)]
    [InlineData("configuration", UserMenuPreferenceDefaults.Tools)]
    [InlineData("internals", UserMenuPreferenceDefaults.Diagnostics)]
    [InlineData("asset", UserMenuPreferenceDefaults.Viewer)]
    public void Normalize_MapsRetiredMenuIDs(string legacyID, string currentID)
    {
        var normalized = UserMenuPreferenceDefaults.Normalize([new UserMenuPreferenceItem(legacyID, false)]);

        Assert.Contains(normalized, item => item.MenuItemID == currentID && !item.Visible);
        Assert.DoesNotContain(normalized, item => item.MenuItemID == legacyID);
    }

    [Theory]
    [InlineData("home")]
    [InlineData("unknown")]
    public void CreatedBuilder_RejectsUnsupportedMenuIDs(string menuItemID)
    {
        var items = UserMenuPreferenceDefaults.CreateVisibleItems()
            .Append(new UserMenuPreferenceItem(menuItemID, true))
            .ToList();

        var result = UserMenuPreferencesCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Create menu preferences",
            items);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ModifiedBuilder_RejectsDuplicateMenuIDs()
    {
        var items = UserMenuPreferenceDefaults.CreateVisibleItems()
            .Append(new UserMenuPreferenceItem(UserMenuPreferenceDefaults.Blotter, false))
            .ToList();

        var result = UserMenuPreferencesModifiedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Modify menu preferences",
            items);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Service_ReturnsAllVisibleFallbackWithoutEvents()
    {
        var service = new UserMenuPreferencesService(new FakeEventRepository());

        var preferences = await service.Get(UserID, EventDate, AuditDate);

        Assert.False(preferences.HasStoredPreferences);
        Assert.All(preferences.Items, item => Assert.True(item.Visible));
        Assert.Equal(Constants.Initialisation.EmptyViewEventID, preferences.LastEventID);
    }

    [Fact]
    public async Task Service_RebuildsModifiedStateAndHonorsAuditDate()
    {
        var firstAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 1, DateTimeKind.Utc));
        var secondAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 2, DateTimeKind.Utc));
        var created = UserMenuPreferencesCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            firstAudit,
            "Create menu preferences",
            UserMenuPreferenceDefaults.CreateVisibleItems()).Value!;
        var modifiedItems = UserMenuPreferenceDefaults.CreateVisibleItems()
            .Select(item => item.MenuItemID == UserMenuPreferenceDefaults.DataList ? item with { Visible = false } : item)
            .ToList();
        var modified = UserMenuPreferencesModifiedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            secondAudit,
            "Modify menu preferences",
            modifiedItems).Value!;
        var service = new UserMenuPreferencesService(new FakeEventRepository(created, modified));

        var beforeModification = await service.Get(UserID, EventDate, firstAudit);
        var afterModification = await service.Get(UserID, EventDate, secondAudit);

        Assert.True(beforeModification.IsVisible(UserMenuPreferenceDefaults.DataList));
        Assert.False(afterModification.IsVisible(UserMenuPreferenceDefaults.DataList));
        Assert.True(afterModification.HasStoredPreferences);
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
