using FolioTrace.Common;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Services;

namespace Repository;

public sealed class InMemoryEventsRepository(MartenEventRepository durableRepository) : IEventRepository
{
    private readonly SemaphoreSlim loadLock = new(1, 1);
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private readonly Lock sync = new();
    private readonly Dictionary<Guid, List<IAuditEventBase>> streams = [];
    private readonly Dictionary<Guid, IAuditEventBase> eventsById = [];
    private readonly List<UnprocessedEventDiagnostic> unprocessedEvents = [];
    private bool isLoaded;

    public EventRepositoryCacheDiagnostics GetCacheDiagnostics()
    {
        lock (sync)
        {
            return new EventRepositoryCacheDiagnostics(
                isLoaded,
                streams.Count,
                eventsById.Count,
                CacheMemoryEstimator.EstimateBytes(eventsById.Values),
                unprocessedEvents.Count,
                unprocessedEvents.OrderByDescending(@event => @event.RecordedAtUtc).Take(10).ToList());
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => EnsureLoadedAsync(cancellationToken);

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await writeLock.WaitAsync(cancellationToken);
        try
        {
            await durableRepository.ClearAsync(cancellationToken);

            lock (sync)
            {
                streams.Clear();
                eventsById.Clear();
                unprocessedEvents.Clear();
                isLoaded = true;
            }
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<T?> LoadAsync<T>(EventID eventId, CancellationToken cancellationToken = default) where T : class, IAuditEventBase
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

            IAuditEventBase? latest = null;
            foreach (var @event in events)
            {
                if (@event is IEventBase timedEvent && timedEvent.EventDateTime.Value > valuationDateTime)
                    continue;

                if (asOfDateTime.HasValue && @event.AuditDateTime.Value > asOfDateTime.Value)
                    continue;

                if (latest is null || CompareEventOrder(@event, latest) > 0)
                    latest = @event;
            }

            return latest?.EventID;
        }
    }

    public async Task<IReadOnlyList<IAuditEventBase>> LoadStreamAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        lock (sync)
        {
            return streams.TryGetValue(streamId, out var events) ? events.ToList() : [];
        }
    }

    public async Task<IReadOnlyList<TEvent>> LoadStreamAsync<TEvent>(Guid streamId, CancellationToken cancellationToken = default)
        where TEvent : class, IAuditEventBase
    {
        var events = await LoadStreamAsync(streamId, cancellationToken);
        return events.OfType<TEvent>().ToList();
    }

    public async Task StartStreamAsync<TAggregate, TEvent>(Guid streamId, IReadOnlyList<TEvent> events, CancellationToken cancellationToken = default)
        where TAggregate : class
        where TEvent : class, IAuditEventBase
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        await EnsureLoadedAsync(cancellationToken);
        await writeLock.WaitAsync(cancellationToken);
        try
        {
            await durableRepository.StartStreamAsync<TAggregate, TEvent>(streamId, events, cancellationToken);

            lock (sync)
            {
                streams[streamId] = [];
                foreach (var @event in events)
                    AddEvent(streamId, @event);
            }
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task AppendAsync<T>(Guid streamId, T @event, CancellationToken cancellationToken = default) where T : class, IAuditEventBase
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        await EnsureLoadedAsync(cancellationToken);
        await writeLock.WaitAsync(cancellationToken);
        try
        {
            await durableRepository.AppendAsync(streamId, @event, cancellationToken);

            lock (sync)
            {
                AddEvent(streamId, @event);
            }
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task AppendAsync(Guid streamId, IEnumerable<IAuditEventBase> events, CancellationToken cancellationToken = default)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        var eventData = events.ToList();
        if (eventData.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null events.", nameof(events));

        if (eventData.Count == 0)
            return;

        await EnsureLoadedAsync(cancellationToken);
        await writeLock.WaitAsync(cancellationToken);
        try
        {
            await durableRepository.AppendAsync(streamId, eventData, cancellationToken);

            lock (sync)
            {
                foreach (var @event in eventData)
                    AddEvent(streamId, @event);
            }
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task AppendWorkflowAsync(IReadOnlyDictionary<Guid, IReadOnlyList<IAuditEventBase>> workflowStreams, StoredFileWrite? storedFile = null, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        await writeLock.WaitAsync(cancellationToken);
        try
        {
            await durableRepository.AppendWorkflowAsync(workflowStreams, storedFile, cancellationToken);
            lock (sync)
                foreach (var stream in workflowStreams)
                    foreach (var @event in stream.Value)
                        AddEvent(stream.Key, @event);
        }
        finally { writeLock.Release(); }
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

            IReadOnlyList<StoredEvent> storedEvents;
            try
            {
                storedEvents = await durableRepository.LoadAllAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                storedEvents = [];

                lock (sync)
                {
                    RecordUnprocessedEvent(null, null, "Unknown", $"Unable to load stored events: {exception.Message}");
                }
            }

            lock (sync)
            {
                streams.Clear();
                eventsById.Clear();

                foreach (var storedEvent in storedEvents)
                {
                    try
                    {
                        var eventData = storedEvent.Event ?? throw new InvalidOperationException("Stored event data is required.");
                        AddEvent(storedEvent.StreamId, eventData);
                    }
                    catch (Exception exception)
                    {
                        RecordUnprocessedEvent(
                            storedEvent.StreamId,
                            TryGetEventId(storedEvent.Event),
                            storedEvent.Event?.GetType().Name ?? "Unknown",
                            exception.Message);
                    }
                }

                isLoaded = true;
            }
        }
        finally
        {
            loadLock.Release();
        }
    }

    private void AddEvent(Guid streamId, IAuditEventBase @event)
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        if (@event.EventID is null)
            throw new InvalidOperationException("EventID is required.");

        if (@event.AuditDateTime is null)
            throw new InvalidOperationException("AuditDateTime is required.");

        if (!streams.TryGetValue(streamId, out var events))
        {
            events = [];
            streams.Add(streamId, events);
        }

        events.Add(@event);
        eventsById[@event.EventID.Value] = @event;
    }

    private void RecordUnprocessedEvent(Guid? streamId, Guid? eventId, string eventType, string reason)
    {
        unprocessedEvents.Add(new UnprocessedEventDiagnostic(
            streamId,
            eventId,
            string.IsNullOrWhiteSpace(eventType) ? "Unknown" : eventType,
            reason,
            DateTime.UtcNow));
    }

    private static Guid? TryGetEventId(IAuditEventBase? @event) => @event?.EventID?.Value;

    private static int CompareEventOrder(IAuditEventBase left, IAuditEventBase right)
    {
        if (left is IEventBase leftTimed && right is IEventBase rightTimed)
        {
            var eventDateComparison = leftTimed.EventDateTime.Value.CompareTo(rightTimed.EventDateTime.Value);
            if (eventDateComparison != 0)
                return eventDateComparison;
        }

        var auditDateComparison = left.AuditDateTime.Value.CompareTo(right.AuditDateTime.Value);
        if (auditDateComparison != 0)
            return auditDateComparison;

        return left.EventID.Value.CompareTo(right.EventID.Value);
    }
}
