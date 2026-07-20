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

    /// <summary>
    /// Loads only the events strictly after <paramref name="afterEventID"/> in the stream's canonical
    /// (EventDateTime, AuditDateTime, EventID) order - used to fetch the delta since a persisted snapshot's
    /// boundary without replaying the whole stream. The default implementation is correct but O(n) (loads the
    /// full stream and filters); InMemoryEventsRepository overrides it with an O(log n + k) version since its
    /// streams are already kept pre-sorted.
    /// </summary>
    async Task<IReadOnlyList<TEvent>> LoadStreamAfterAsync<TEvent>(Guid streamId, EventID afterEventID, CancellationToken cancellationToken = default)
        where TEvent : class, IAuditEventBase
    {
        if (afterEventID is null)
            throw new ArgumentNullException(nameof(afterEventID));

        var boundary = await LoadAsync<IAuditEventBase>(afterEventID, cancellationToken);
        var events = await LoadStreamAsync<TEvent>(streamId, cancellationToken);

        return boundary is null
            ? events
            : events.Where(@event => EventOrderComparer.Compare(@event, boundary) > 0).ToList();
    }

    Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
        where TAggregate : class
        where TEvent : class, IAuditEventBase;

    Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IAuditEventBase;

    Task AppendAsync(Guid streamId, IEnumerable<IAuditEventBase> events, CancellationToken cancellationToken = default);

    Task AppendWorkflowAsync(IReadOnlyDictionary<Guid, IReadOnlyList<IAuditEventBase>> streams, StoredFileWrite? storedFile = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Atomic workflow writes are not supported by this repository.");
}
