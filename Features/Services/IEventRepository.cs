using FolioTrace.Common;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Repository;

public interface IEventRepository
{
    EventRepositoryCacheDiagnostics GetCacheDiagnostics();

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);

    Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IAuditEventBase;

    Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default);

    Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IAuditEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
        where TEvent : class, IAuditEventBase;

    Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
        where TAggregate : class
        where TEvent : class, IAuditEventBase;

    Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IAuditEventBase;

    Task AppendAsync(Guid streamId, IEnumerable<IAuditEventBase> events, CancellationToken cancellationToken = default);

    Task AppendWorkflowAsync(IReadOnlyDictionary<Guid, IReadOnlyList<IAuditEventBase>> streams, StoredFilePayload? storedFile = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Atomic workflow writes are not supported by this repository.");

    Task<StoredFilePayload?> LoadStoredFileAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Stored files are not supported by this repository.");
}
