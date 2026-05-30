using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace Test;

public sealed class UserValuationPreferencesTests
{
    private static readonly UserID UserID = new(Guid.Parse("4a81df88-83d8-4cb7-bbf2-c1e544a56aef"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 1, DateTimeKind.Utc));

    [Fact]
    public void CreatedBuilder_AcceptsDefaultPreferences()
    {
        var result = UserValuationPreferencesCreatedEventBuilder.CreateSeed(
            Guid.NewGuid(),
            UserID,
            EventDate,
            AuditDate,
            "Create valuation preferences",
            UserValuationPreferenceDefaults.ValuationDateOption,
            UserValuationPreferenceDefaults.ValuationDateBasis,
            UserValuationPreferenceDefaults.ShowZeroBalances);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Value);
        Assert.Equal(UserValuationDateOption.TodayEndOfDay, result.Value!.ValuationDateOption);
        Assert.Equal(ValuationDateBasis.EventDateTime, result.Value.ValuationDateBasis);
        Assert.False(result.Value.ShowZeroBalances);
    }

    [Fact]
    public void ModifiedBuilder_RejectsUnsupportedEnums()
    {
        var result = UserValuationPreferencesModifiedEventBuilder.CreateSeed(
            Guid.NewGuid(),
            UserID,
            EventDate,
            AuditDate,
            "Modify valuation preferences",
            (UserValuationDateOption)999,
            (ValuationDateBasis)999,
            true);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Service_ReturnsDefaultFallbackWithoutEvents()
    {
        var service = new UserValuationPreferencesService(new FakeEventRepository());

        var preferences = await service.Get(UserID, EventDate, AuditDate);

        Assert.False(preferences.HasStoredPreferences);
        Assert.Equal(UserValuationPreferenceDefaults.ValuationDateOption, preferences.ValuationDateOption);
        Assert.Equal(UserValuationPreferenceDefaults.ValuationDateBasis, preferences.ValuationDateBasis);
        Assert.Equal(UserValuationPreferenceDefaults.ShowZeroBalances, preferences.ShowZeroBalances);
        Assert.Equal(Constants.Initialisation.EmptyViewEventID, preferences.LastEventID);
    }

    [Fact]
    public async Task Service_RebuildsModifiedStateAndHonorsAuditDate()
    {
        var firstAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 1, DateTimeKind.Utc));
        var secondAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 2, DateTimeKind.Utc));
        var created = UserValuationPreferencesCreatedEventBuilder.CreateSeed(
            Guid.NewGuid(),
            UserID,
            EventDate,
            firstAudit,
            "Create valuation preferences",
            UserValuationDateOption.TodayEndOfDay,
            ValuationDateBasis.EventDateTime,
            false).Value!;
        var modified = UserValuationPreferencesModifiedEventBuilder.CreateSeed(
            Guid.NewGuid(),
            UserID,
            EventDate,
            secondAudit,
            "Modify valuation preferences",
            UserValuationDateOption.LastMonthEndOfDay,
            ValuationDateBasis.SettlementDateTime,
            true).Value!;
        var service = new UserValuationPreferencesService(new FakeEventRepository(created, modified));

        var beforeModification = await service.Get(UserID, EventDate, firstAudit);
        var afterModification = await service.Get(UserID, EventDate, secondAudit);

        Assert.Equal(UserValuationDateOption.TodayEndOfDay, beforeModification.ValuationDateOption);
        Assert.False(beforeModification.ShowZeroBalances);
        Assert.Equal(UserValuationDateOption.LastMonthEndOfDay, afterModification.ValuationDateOption);
        Assert.Equal(ValuationDateBasis.SettlementDateTime, afterModification.ValuationDateBasis);
        Assert.True(afterModification.ShowZeroBalances);
        Assert.True(afterModification.HasStoredPreferences);
    }

    private sealed class FakeEventRepository(params IEventBase[] events) : IEventRepository
    {
        private readonly List<IEventBase> events = events.ToList();

        public EventRepositoryCacheDiagnostics GetCacheDiagnostics() => new(true, 1, this.events.Count, 0, 0, []);

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            events.Clear();
            return Task.CompletedTask;
        }

        public Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IEventBase =>
            Task.FromResult(events.SingleOrDefault(@event => @event.EventID == eventId) as T);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult(events.LastOrDefault()?.EventID);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default)
        {
            var latest = events
                .Where(@event => @event.EventDateTime.Value <= valuationDateTime && (!asOfDateTime.HasValue || @event.AuditDateTime.Value <= asOfDateTime.Value))
                .OrderBy(@event => @event.EventDateTime.Value)
                .ThenBy(@event => @event.AuditDateTime.Value)
                .ThenBy(@event => @event.EventID.Value)
                .LastOrDefault();

            return Task.FromResult(latest?.EventID);
        }

        public Task<IReadOnlyList<IEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<IEventBase>)events.ToList());

        public Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
            where TEvent : class, IEventBase =>
            Task.FromResult((IReadOnlyList<TEvent>)events.OfType<TEvent>().ToList());

        public Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
            where TAggregate : class
            where TEvent : class, IEventBase
        {
            this.events.Clear();
            this.events.AddRange(events);
            return Task.CompletedTask;
        }

        public Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IEventBase
        {
            events.Add(@event);
            return Task.CompletedTask;
        }

        public Task AppendAsync(Guid streamId, IEnumerable<IEventBase> events, CancellationToken cancellationToken = default)
        {
            this.events.AddRange(events);
            return Task.CompletedTask;
        }
    }
}
