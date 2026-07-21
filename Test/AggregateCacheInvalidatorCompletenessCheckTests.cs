using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Microsoft.Extensions.DependencyInjection;
using Repository;
using Services;

namespace Test;

public sealed class AggregateCacheInvalidatorCompletenessCheckTests
{
    [Fact]
    public void Validate_DoesNotThrow_ForTheRealServiceRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventRepository>(new FakeEventRepository());
        services.AddSingleton<IAggregateSnapshotRepository>(new FakeAggregateSnapshotRepository());
        services.AddFolioTraceServices();
        var provider = services.BuildServiceProvider();

        var exception = Record.Exception(() => AggregateCacheInvalidatorCompletenessCheck.Validate(provider));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_Throws_WhenAFamilyMemberHasNoRegisteredInvalidator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAggregateCacheInvalidator>(new AggregateCacheInvalidator<AccountCreatedEvent>(_ => 0));
        var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => AggregateCacheInvalidatorCompletenessCheck.Validate(provider));

        Assert.Contains(nameof(AccountModifiedEvent), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenTheOnlyInvalidatorIsTheUniversalNotificationCatchAll()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAggregateCacheInvalidator>(new AggregateCacheInvalidator<IAuditEventBase>(_ => 0));
        var provider = services.BuildServiceProvider();

        var exception = Record.Exception(() => AggregateCacheInvalidatorCompletenessCheck.Validate(provider));

        Assert.Null(exception);
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
