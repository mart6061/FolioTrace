using FolioTrace.Common;
using FolioTrace.Types;

namespace Repository;

public interface IEventRepository
{
    EventRepositoryCacheDiagnostics GetCacheDiagnostics();

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);

    Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IEventBase;

    Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default);

    Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
        where TEvent : class, IEventBase;

    Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
        where TAggregate : class
        where TEvent : class, IEventBase;

    Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IEventBase;

    Task AppendAsync(Guid streamId, IEnumerable<IEventBase> events, CancellationToken cancellationToken = default);
}
