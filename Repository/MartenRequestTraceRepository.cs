using Marten;
using Marten.Linq;
using Npgsql;
using System.Linq;

namespace Repository;

public sealed class MartenRequestTraceRepository(
    IDocumentStore store,
    NpgsqlDataSource dataSource) : IRequestTraceRepository
{
    internal const string PurgeLegacyUiEventsSql = """
        delete from mt_doc_requesttraceevent
        where upper(coalesce(data ->> 'Source', data ->> 'source', '')) = 'UI';
        """;

    public Task AppendAsync(RequestTraceEvent traceEvent, CancellationToken cancellationToken = default) =>
        AppendAsync([traceEvent], cancellationToken);

    public async Task AppendAsync(IReadOnlyCollection<RequestTraceEvent> traceEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(traceEvents);
        if (traceEvents.Count == 0)
            return;

        await using var session = store.LightweightSession();

        foreach (var traceEvent in traceEvents)
            session.Store(traceEvent);

        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task<RequestTrace?> LoadAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        await using var session = store.QuerySession();

        var events = await session.Query<RequestTraceEvent>()
            .Where(traceEvent => traceEvent.RequestId == requestId)
            .OrderBy(traceEvent => traceEvent.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        return events.Count == 0 ? null : Group(requestId, events);
    }

    public async Task<RequestTraceSearchResult> SearchAsync(RequestTraceSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var page = Math.Max(1, criteria.Page);
        var pageSize = Math.Clamp(criteria.PageSize, 1, 200);

        await using var session = store.QuerySession();

        IQueryable<RequestTraceEvent> query = session.Query<RequestTraceEvent>();

        if (criteria.FromUtc.HasValue)
            query = query.Where(traceEvent => traceEvent.RecordedAtUtc >= criteria.FromUtc.Value);

        if (criteria.ToUtc.HasValue)
            query = query.Where(traceEvent => traceEvent.RecordedAtUtc <= criteria.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(criteria.Method))
        {
            var method = criteria.Method.Trim().ToUpperInvariant();
            query = query.Where(traceEvent => traceEvent.Method == method);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Path))
        {
            var path = criteria.Path.Trim();
            query = query.Where(traceEvent => traceEvent.Path.Contains(path));
        }

        if (criteria.StatusCode.HasValue)
            query = query.Where(traceEvent => traceEvent.StatusCode == criteria.StatusCode.Value);

        if (criteria.MinimumDurationMilliseconds.HasValue)
            query = query.Where(traceEvent => traceEvent.DurationMilliseconds >= criteria.MinimumDurationMilliseconds.Value);

        if (criteria.MaximumDurationMilliseconds.HasValue)
            query = query.Where(traceEvent => traceEvent.DurationMilliseconds <= criteria.MaximumDurationMilliseconds.Value);

        if (!string.IsNullOrWhiteSpace(criteria.Text))
        {
            var searchText = criteria.Text.Trim();
            query = query.Where(traceEvent =>
                traceEvent.Method.Contains(searchText) ||
                traceEvent.Path.Contains(searchText) ||
                traceEvent.QueryString.Contains(searchText) ||
                (traceEvent.Message != null && traceEvent.Message.Body != null && traceEvent.Message.Body.Contains(searchText)) ||
                (traceEvent.ExceptionMessage != null && traceEvent.ExceptionMessage.Contains(searchText)) ||
                (traceEvent.StackTrace != null && traceEvent.StackTrace.Contains(searchText)) ||
                (traceEvent.LogMessage != null && traceEvent.LogMessage.Contains(searchText)));
        }

        var groupedQuery = query
            .GroupBy(traceEvent => traceEvent.RequestId)
            .Select(group => new RequestTracePageCandidate
            {
                RequestId = group.Key,
                RecordedAtUtc = group.Max(traceEvent => traceEvent.RecordedAtUtc)
            });

        var totalCount = await groupedQuery.CountAsync(cancellationToken);
        var requestIds = await groupedQuery
            .OrderByDescending(candidate => candidate.RecordedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(candidate => candidate.RequestId)
            .ToListAsync(cancellationToken);

        if (requestIds.Count == 0)
            return new RequestTraceSearchResult([], totalCount, page, pageSize);

        var events = await session.Query<RequestTraceEvent>()
            .Where(traceEvent => requestIds.Contains(traceEvent.RequestId))
            .OrderBy(traceEvent => traceEvent.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        var items = events
            .GroupBy(traceEvent => traceEvent.RequestId)
            .Select(group => Group(group.Key, group))
            .OrderByDescending(trace => trace.StartedAtUtc)
            .ToList();

        return new RequestTraceSearchResult(items, totalCount, page, pageSize);
    }

    public async Task<RequestTracePurgeResult> PurgeAsync(DateTime? beforeUtc, CancellationToken cancellationToken = default)
    {
        await using var session = store.LightweightSession();

        var query = beforeUtc.HasValue
            ? session.Query<RequestTraceEvent>().Where(traceEvent => traceEvent.RecordedAtUtc < beforeUtc.Value)
            : session.Query<RequestTraceEvent>();
        var deletedCount = query.Count();

        if (beforeUtc.HasValue)
            session.DeleteWhere<RequestTraceEvent>(traceEvent => traceEvent.RecordedAtUtc < beforeUtc.Value);
        else
            session.DeleteWhere<RequestTraceEvent>(_ => true);

        await session.SaveChangesAsync(cancellationToken);

        return new RequestTracePurgeResult(deletedCount);
    }

    public async Task<int> PurgeLegacyUiEventsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(PurgeLegacyUiEventsSql, connection);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<RequestTraceSettings?> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var session = store.QuerySession();

        var document = await session.LoadAsync<RequestTraceSettingsDocument>(
            RequestTraceConstants.SettingsDocumentId,
            cancellationToken);

        return document?.Settings;
    }

    public async Task StoreSettingsAsync(RequestTraceSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        await using var session = store.LightweightSession();

        session.Store(new RequestTraceSettingsDocument
        {
            Id = RequestTraceConstants.SettingsDocumentId,
            Settings = settings,
            ModifiedAtUtc = DateTime.UtcNow
        });
        await session.SaveChangesAsync(cancellationToken);
    }

    private static RequestTrace Group(Guid requestId, IEnumerable<RequestTraceEvent> traceEvents)
    {
        var events = traceEvents.ToList();
        var request = PreferSource(events, RequestTraceEventKinds.Request);
        var response = PreferSource(events, RequestTraceEventKinds.Response);
        var exception = events
            .Where(traceEvent => traceEvent.Kind == RequestTraceEventKinds.Exception)
            .OrderByDescending(traceEvent => traceEvent.RecordedAtUtc)
            .FirstOrDefault();
        var logs = events
            .Where(traceEvent => traceEvent.Kind == RequestTraceEventKinds.Log)
            .OrderBy(traceEvent => traceEvent.RecordedAtUtc)
            .Select(traceEvent => new TraceLogEntry(
                traceEvent.RecordedAtUtc,
                traceEvent.LogLevel ?? string.Empty,
                traceEvent.LogCategory ?? string.Empty,
                traceEvent.LogEventId,
                traceEvent.LogMessage ?? string.Empty,
                traceEvent.ExceptionType,
                traceEvent.ExceptionMessage,
                traceEvent.StackTrace))
            .ToList();
        var startedAtUtc = request?.StartedAtUtc ??
            request?.RecordedAtUtc ??
            events.Min(traceEvent => traceEvent.RecordedAtUtc);
        var completedAtUtc = response?.CompletedAtUtc ?? response?.RecordedAtUtc;

        return new RequestTrace
        {
            RequestId = requestId,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            DurationMilliseconds = response?.DurationMilliseconds,
            Method = FirstPopulated(request?.Method, response?.Method, exception?.Method),
            Path = FirstPopulated(request?.Path, response?.Path, exception?.Path),
            QueryString = FirstPopulated(request?.QueryString, response?.QueryString, exception?.QueryString),
            StatusCode = response?.StatusCode,
            HasResponse = response is not null,
            HasException = exception is not null,
            LogCount = logs.Count,
            Request = request?.Message,
            Response = response?.Message,
            Exception = exception is null
                ? null
                : new RequestTraceException(
                    exception.RecordedAtUtc,
                    exception.ExceptionType,
                    exception.ExceptionMessage,
                    exception.StackTrace),
            Logs = logs
        };
    }

    private sealed record RequestTracePageCandidate
    {
        public Guid RequestId { get; init; }

        public DateTime RecordedAtUtc { get; init; }
    }

    private static RequestTraceEvent? PreferSource(IEnumerable<RequestTraceEvent> traceEvents, string kind) =>
        traceEvents
            .Where(traceEvent => traceEvent.Kind == kind)
            .OrderBy(traceEvent => traceEvent.RecordedAtUtc)
            .FirstOrDefault();

    private static string FirstPopulated(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}
