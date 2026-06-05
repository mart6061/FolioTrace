using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace Test;

public sealed class ReferenceDataServiceTests
{
    [Fact]
    public void ListedReferenceDataServices_ImplementCommonInterface()
    {
        var repository = new FakeEventRepository();

        AssertReferenceService<Accounts, AccountServiceDiagnostics>(new AccountService(repository));
        AssertReferenceService<Brokers, BrokerServiceDiagnostics>(new BrokerService(repository));
        AssertReferenceService<Countries, CountryServiceDiagnostics>(new CountryService(repository));
        AssertReferenceService<Currencies, CurrencyServiceDiagnostics>(new CurrencyService(repository));
        AssertReferenceService<FXs, FXServiceDiagnostics>(new FXService(repository));
        AssertReferenceService<Instruments, InstrumentServiceDiagnostics>(new InstrumentService(repository));
    }

    [Fact]
    public void CurrentHelper_UsesLocalEndOfToday()
    {
        var expected = DateTime.Now.Date.AddDays(1).AddTicks(-1);

        var current = ReferenceDataCurrent.EndOfToday();

        Assert.Equal(expected, current.Value);
    }

    private static void AssertReferenceService<TAggregate, TDiagnostics>(IReferenceDataService<TAggregate, TDiagnostics> service)
        where TAggregate : IAggregate =>
        Assert.NotNull(service);

    private sealed class FakeEventRepository : IEventRepository
    {
        public EventRepositoryCacheDiagnostics GetCacheDiagnostics() => new(true, 0, 0, 0, 0, []);

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IEventBase =>
            Task.FromResult<T?>(null);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventID?>(null);

        public Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventID?>(null);

        public Task<IReadOnlyList<IEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IEventBase>>([]);

        public Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
            where TEvent : class, IEventBase =>
            Task.FromResult<IReadOnlyList<TEvent>>([]);

        public Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
            where TAggregate : class
            where TEvent : class, IEventBase =>
            Task.CompletedTask;

        public Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IEventBase =>
            Task.CompletedTask;

        public Task AppendAsync(Guid streamId, IEnumerable<IEventBase> events, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
