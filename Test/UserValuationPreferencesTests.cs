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
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Create valuation preferences",
            UserValuationPreferenceDefaults.ValuationDateOption,
            UserValuationPreferenceDefaults.HoldingDateBasis,
            UserValuationPreferenceDefaults.ShowZeroBalances);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Value);
        Assert.Equal(UserValuationDateOption.TodayEndOfDay, result.Value!.ValuationDateOption);
        Assert.Equal(UserValuationPreferenceDefaults.StartValuationDateOption, result.Value.StartValuationDateOption);
        Assert.Equal(UserValuationPreferenceDefaults.EndValuationDateOption, result.Value.EndValuationDateOption);
        Assert.Equal(HoldingDateBasis.EventDateTime, result.Value.HoldingDateBasis);
        Assert.False(result.Value.ShowZeroBalances);
    }

    [Fact]
    public void ModifiedBuilder_RejectsUnsupportedEnums()
    {
        var result = UserValuationPreferencesModifiedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            AuditDate,
            "Modify valuation preferences",
            (UserValuationDateOption)999,
            (HoldingDateBasis)999,
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
        Assert.Equal(UserValuationPreferenceDefaults.StartValuationDateOption, preferences.StartValuationDateOption);
        Assert.Equal(UserValuationPreferenceDefaults.EndValuationDateOption, preferences.EndValuationDateOption);
        Assert.Equal(UserValuationPreferenceDefaults.HoldingDateBasis, preferences.HoldingDateBasis);
        Assert.Equal(UserValuationPreferenceDefaults.ShowZeroBalances, preferences.ShowZeroBalances);
        Assert.Equal(Constants.Initialisation.EmptyViewEventID, preferences.LastEventID);
    }

    [Fact]
    public async Task Service_RebuildsModifiedStateAndHonorsAuditDate()
    {
        var firstAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 1, DateTimeKind.Utc));
        var secondAudit = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 2, DateTimeKind.Utc));
        var created = UserValuationPreferencesCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            firstAudit,
            "Create valuation preferences",
            UserValuationDateOption.YesterdayEndOfDay,
            UserValuationDateOption.TodayEndOfDay,
            HoldingDateBasis.EventDateTime,
            false).Value!;
        var modified = UserValuationPreferencesModifiedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            UserID,
            EventDate,
            secondAudit,
            "Modify valuation preferences",
            UserValuationDateOption.LastQuarterEndOfDay,
            UserValuationDateOption.LastMonthEndOfDay,
            HoldingDateBasis.SettlementDateTime,
            true).Value!;
        var service = new UserValuationPreferencesService(new FakeEventRepository(created, modified));

        var beforeModification = await service.Get(UserID, EventDate, firstAudit);
        var afterModification = await service.Get(UserID, EventDate, secondAudit);

        Assert.Equal(UserValuationDateOption.TodayEndOfDay, beforeModification.ValuationDateOption);
        Assert.Equal(UserValuationDateOption.YesterdayEndOfDay, beforeModification.StartValuationDateOption);
        Assert.Equal(UserValuationDateOption.TodayEndOfDay, beforeModification.EndValuationDateOption);
        Assert.False(beforeModification.ShowZeroBalances);
        Assert.Equal(UserValuationDateOption.LastMonthEndOfDay, afterModification.ValuationDateOption);
        Assert.Equal(UserValuationDateOption.LastQuarterEndOfDay, afterModification.StartValuationDateOption);
        Assert.Equal(UserValuationDateOption.LastMonthEndOfDay, afterModification.EndValuationDateOption);
        Assert.Equal(HoldingDateBasis.SettlementDateTime, afterModification.HoldingDateBasis);
        Assert.True(afterModification.ShowZeroBalances);
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
