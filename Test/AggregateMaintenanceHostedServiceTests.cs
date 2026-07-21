using API;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace Test;

public sealed class AggregateMaintenanceHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_RunsOnceImmediately_BeforeThePeriodicTimerWouldEverTick()
    {
        var readinessState = new ApiReadinessState();
        readinessState.MarkReady();
        var coordinator = CreateCoordinator(new AggregateMaintenanceOptions
        {
            Enabled = true,
            // A long periodic delay - if startup seeding relied solely on the periodic timer, this run would
            // never complete within the test's lifetime.
            PeriodicDelay = TimeSpan.FromHours(1),
            DateWindows = new AggregateMaintenanceDateWindowOptions()
        });
        var hostedService = new AggregateMaintenanceHostedService(coordinator.Options, coordinator.Coordinator, readinessState);

        using var cancellation = new CancellationTokenSource();
        var executeTask = hostedService.StartAsync(cancellation.Token);

        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (coordinator.Coordinator.GetDiagnostics() is { LastTrigger: "Startup", Status: "Succeeded" })
                break;

            await Task.Delay(10);
        }

        Assert.Equal("Startup", coordinator.Coordinator.GetDiagnostics().LastTrigger);
        Assert.Equal("Succeeded", coordinator.Coordinator.GetDiagnostics().Status);

        await cancellation.CancelAsync();
        await Task.WhenAny(executeTask, Task.Delay(TimeSpan.FromSeconds(2)));
    }

    private static (AggregateMaintenanceOptions Options, AggregateMaintenanceCoordinator Coordinator) CreateCoordinator(AggregateMaintenanceOptions options)
    {
        var eventRepository = new FakeEventRepository();
        var accountService = new AccountService(eventRepository);
        var brokerService = new BrokerService(eventRepository);
        var countryService = new CountryService(eventRepository);
        var currencyService = new CurrencyService(eventRepository);
        var fxService = new FXService(eventRepository);
        var fxRateService = new FXRateService(eventRepository, new FakeFXRateReadModelRepository());
        var holdingService = new HoldingService(eventRepository);
        var instrumentService = new InstrumentService(eventRepository);
        var instrumentValueService = new InstrumentValueService(eventRepository);
        var holdingPositionService = new HoldingPositionService(eventRepository, holdingService, accountService, instrumentService, new FakeAggregateSnapshotRepository());

        var coordinator = new AggregateMaintenanceCoordinator(
            options,
            accountService,
            brokerService,
            countryService,
            currencyService,
            fxService,
            fxRateService,
            holdingService,
            holdingPositionService,
            instrumentService,
            instrumentValueService,
            new AggregateUpdateNotificationService());

        return (options, coordinator);
    }

    private sealed class FakeFXRateReadModelRepository : IFXRateReadModelRepository
    {
        public Task<FXRates?> LoadAsync(EventDateTime valuationDateTime, CancellationToken cancellationToken = default) =>
            Task.FromResult<FXRates?>(null);

        public Task RebuildAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RebuildPairAsync(CurrencyPair pair, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
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
