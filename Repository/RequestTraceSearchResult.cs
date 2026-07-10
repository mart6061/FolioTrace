namespace Repository;

public sealed record RequestTraceSearchResult(
    IReadOnlyList<RequestTrace> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record RequestTracePurgeResult(int DeletedCount);
