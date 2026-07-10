namespace Repository;

public interface IRequestTraceRepository
{
    Task AppendAsync(RequestTraceEvent traceEvent, CancellationToken cancellationToken = default);

    Task<RequestTrace?> LoadAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<RequestTraceSearchResult> SearchAsync(RequestTraceSearchCriteria criteria, CancellationToken cancellationToken = default);

    Task<RequestTracePurgeResult> PurgeAsync(DateTime? beforeUtc, CancellationToken cancellationToken = default);

    Task<RequestTraceSettings?> LoadSettingsAsync(CancellationToken cancellationToken = default);

    Task StoreSettingsAsync(RequestTraceSettings settings, CancellationToken cancellationToken = default);
}
