namespace Repository;

public sealed record RequestTraceSearchCriteria(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? Method,
    string? Path,
    int? StatusCode,
    int? MinimumDurationMilliseconds,
    int? MaximumDurationMilliseconds,
    string? Text,
    int Page,
    int PageSize);
