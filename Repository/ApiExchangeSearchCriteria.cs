namespace Repository;

public sealed record ApiExchangeSearchCriteria(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Method = null,
    string? Path = null,
    int? StatusCode = null,
    int? MinimumDurationMilliseconds = null,
    int? MaximumDurationMilliseconds = null,
    string? Text = null,
    int Page = 1,
    int PageSize = 50);
