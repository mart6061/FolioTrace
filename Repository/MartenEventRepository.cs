using FolioTrace.Common;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Marten;

namespace Repository;

public sealed class MartenEventRepository(IDocumentStore store)
{
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await store.Advanced.Clean.DeleteAllEventDataAsync(cancellationToken);
        await store.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(StoredFilePayload), cancellationToken);
    }

    public async Task<IReadOnlyList<StoredEvent>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await using var session = store.QuerySession();

        var rawEvents = session.Events.QueryAllRawEvents().ToList();
        return rawEvents
            .Where(@event => @event.Data is IAuditEventBase)
            .Select(@event => new StoredEvent(@event.StreamId, (IAuditEventBase)@event.Data))
            .ToList();
    }

    public async Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IAuditEventBase
    {
        if (eventId is null)
            throw new ArgumentNullException(nameof(eventId));

        await using var session = store.QuerySession();

        var @event = await session.Events.LoadAsync<T>(eventId.Value, cancellationToken);
        return @event?.Data;
    }

    public async Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
        where TAggregate : class
        where TEvent : class, IAuditEventBase
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        if (events.Count == 0)
            return;

        await using var session = store.LightweightSession();

        session.Events.StartStream<TAggregate>(streamId, events.Cast<object>());
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IAuditEventBase
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        await using var session = store.LightweightSession();

        session.Events.Append(streamId, @event);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendAsync(Guid streamId, IReadOnlyList<IAuditEventBase> events, CancellationToken cancellationToken = default)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        if (events.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null events.", nameof(events));

        if (events.Count == 0)
            return;

        await using var session = store.LightweightSession();

        session.Events.Append(streamId, events.Cast<object>());
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendWorkflowAsync(IReadOnlyDictionary<Guid, IReadOnlyList<IAuditEventBase>> streams, StoredFilePayload? storedFile = null, CancellationToken cancellationToken = default)
    {
        await using var session = store.LightweightSession();
        foreach (var stream in streams)
            if (stream.Value.Count > 0)
                session.Events.Append(stream.Key, stream.Value.Cast<object>());
        if (storedFile is not null)
            session.Store(storedFile);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredFilePayload?> LoadStoredFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var session = store.QuerySession();
        return await session.LoadAsync<StoredFilePayload>(id, cancellationToken);
    }
}
