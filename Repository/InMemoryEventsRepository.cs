using FolioTrace.Common;
using FolioTrace.Types;

namespace Repository;

public sealed class InMemoryEventsRepository(MartenEventRepository durableRepository) : IEventRepository
{
    private readonly SemaphoreSlim loadLock = new(1, 1);
    private readonly Lock sync = new();
    private readonly Dictionary<Guid, List<IEventBase>> streams = [];
    private readonly Dictionary<Guid, IEventBase> eventsById = [];
    private bool isLoaded;

    public EventRepositoryCacheDiagnostics GetCacheDiagnostics()
    {
        lock (sync)
        {
            return new EventRepositoryCacheDiagnostics(
                isLoaded,
                streams.Count,
                eventsById.Count);
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => EnsureLoadedAsync(cancellationToken);

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await durableRepository.ClearAsync(cancellationToken);

        lock (sync)
        {
            streams.Clear();
            eventsById.Clear();
            isLoaded = true;
        }
    }

    public async Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IEventBase
    {
        if (eventId is null)
            throw new ArgumentNullException(nameof(eventId));

        await EnsureLoadedAsync(cancellationToken);

        lock (sync)
        {
            return eventsById.TryGetValue(eventId.Value, out var @event) ? @event as T : null;
        }
    }

    public async Task<EventID?> GetLastEventIDAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        lock (sync)
        {
            return streams.TryGetValue(streamId, out var events) && events.Count > 0
                ? events[^1].EventID
                : null;
        }
    }

    public async Task<EventID?> GetLastEventIDAsync(Guid streamId, DateTime valuationDateTime, DateTime? asOfDateTime = null, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        lock (sync)
        {
            if (!streams.TryGetValue(streamId, out var events))
                return null;

            IEventBase? latest = null;
            foreach (var @event in events)
            {
                if (@event.EventDateTime.Value > valuationDateTime || (asOfDateTime.HasValue && @event.AuditDateTime.Value > asOfDateTime.Value))
                    continue;

                if (latest is null || CompareEventOrder(@event, latest) > 0)
                    latest = @event;
            }

            return latest?.EventID;
        }
    }

    public async Task<IReadOnlyList<IEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        lock (sync)
        {
            return streams.TryGetValue(streamId, out var events) ? events.ToList() : [];
        }
    }

    public async Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
        where TEvent : class, IEventBase
    {
        var events = await LoadStreamAsync(streamId, cancellationToken);
        return events.OfType<TEvent>().ToList();
    }

    public async Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
        where TAggregate : class
        where TEvent : class, IEventBase
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        await EnsureLoadedAsync(cancellationToken);
        await durableRepository.StartStreamAsync<TAggregate, TEvent>(streamId, events, cancellationToken);

        lock (sync)
        {
            streams[streamId] = [];
            foreach (var @event in events)
                AddEvent(streamId, @event);
        }
    }

    public async Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IEventBase
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        await EnsureLoadedAsync(cancellationToken);
        await durableRepository.AppendAsync(streamId, @event, cancellationToken);

        lock (sync)
        {
            AddEvent(streamId, @event);
        }
    }

    public async Task AppendAsync(Guid streamId, IEnumerable<IEventBase> events, CancellationToken cancellationToken = default)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        var eventData = events.ToList();
        if (eventData.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null events.", nameof(events));

        if (eventData.Count == 0)
            return;

        await EnsureLoadedAsync(cancellationToken);
        await durableRepository.AppendAsync(streamId, eventData, cancellationToken);

        lock (sync)
        {
            foreach (var @event in eventData)
                AddEvent(streamId, @event);
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (isLoaded)
            return;

        await loadLock.WaitAsync(cancellationToken);
        try
        {
            if (isLoaded)
                return;

            var storedEvents = await durableRepository.LoadAllAsync(cancellationToken);

            lock (sync)
            {
                streams.Clear();
                eventsById.Clear();

                foreach (var storedEvent in storedEvents)
                    AddEvent(storedEvent.StreamId, storedEvent.Event);

                isLoaded = true;
            }
        }
        finally
        {
            loadLock.Release();
        }
    }

    private void AddEvent(Guid streamId, IEventBase @event)
    {
        if (!streams.TryGetValue(streamId, out var events))
        {
            events = [];
            streams.Add(streamId, events);
        }

        events.Add(@event);
        eventsById[@event.EventID.Value] = @event;
    }

    private static int CompareEventOrder(IEventBase left, IEventBase right)
    {
        var eventDateComparison = left.EventDateTime.Value.CompareTo(right.EventDateTime.Value);
        if (eventDateComparison != 0)
            return eventDateComparison;

        var auditDateComparison = left.AuditDateTime.Value.CompareTo(right.AuditDateTime.Value);
        if (auditDateComparison != 0)
            return auditDateComparison;

        return left.EventID.Value.CompareTo(right.EventID.Value);
    }
}
