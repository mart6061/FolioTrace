using Marten;
using Marten.Linq;
using System.Linq;

namespace Repository;

public sealed class MartenRequestTraceRepository(IDocumentStore store) : IRequestTraceRepository
{
    public async Task AppendAsync(RequestTraceEvent traceEvent, CancellationToken cancellationToken = default)
    {
        if (traceEvent is null)
            throw new ArgumentNullException(nameof(traceEvent));

        await using var session = store.LightweightSession();

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
        if (criteria is null)
            throw new ArgumentNullException(nameof(criteria));

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

        var events = await query
            .OrderByDescending(traceEvent => traceEvent.RecordedAtUtc)
            .Take(Math.Max(page * pageSize * 10, pageSize))
            .ToListAsync(cancellationToken);

        var grouped = events
            .GroupBy(traceEvent => traceEvent.RequestId)
            .Select(group => Group(group.Key, group.OrderBy(traceEvent => traceEvent.RecordedAtUtc)))
            .Where(trace => Matches(criteria, trace))
            .OrderByDescending(trace => trace.StartedAtUtc)
            .ToList();

        var totalCount = grouped.Count;
        var items = grouped
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            Source = string.Join("+", events.Select(traceEvent => traceEvent.Source).Where(source => !string.IsNullOrWhiteSpace(source)).Distinct()),
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

    private static RequestTraceEvent? PreferSource(IEnumerable<RequestTraceEvent> traceEvents, string kind) =>
        traceEvents
            .Where(traceEvent => traceEvent.Kind == kind)
            .OrderBy(traceEvent => traceEvent.Source == RequestTraceSources.Api ? 0 : 1)
            .ThenBy(traceEvent => traceEvent.RecordedAtUtc)
            .FirstOrDefault();

    private static bool Matches(RequestTraceSearchCriteria criteria, RequestTrace trace)
    {
        if (criteria.MinimumDurationMilliseconds.HasValue &&
            (!trace.DurationMilliseconds.HasValue || trace.DurationMilliseconds.Value < criteria.MinimumDurationMilliseconds.Value))
            return false;

        if (criteria.MaximumDurationMilliseconds.HasValue &&
            (!trace.DurationMilliseconds.HasValue || trace.DurationMilliseconds.Value > criteria.MaximumDurationMilliseconds.Value))
            return false;

        if (string.IsNullOrWhiteSpace(criteria.Text))
            return true;

        var text = criteria.Text.Trim();

        return Contains(trace.Method, text) ||
            Contains(trace.Path, text) ||
            Contains(trace.QueryString, text) ||
            Contains(trace.Request?.Body, text) ||
            Contains(trace.Response?.Body, text) ||
            Contains(trace.Exception?.ExceptionMessage, text) ||
            Contains(trace.Exception?.StackTrace, text) ||
            trace.Logs.Any(log => Contains(log.Message, text) || Contains(log.ExceptionMessage, text) || Contains(log.StackTrace, text));
    }

    private static bool Contains(string? value, string text) =>
        !string.IsNullOrEmpty(value) && value.Contains(text, StringComparison.OrdinalIgnoreCase);

    private static string FirstPopulated(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}
