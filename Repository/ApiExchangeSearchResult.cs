namespace Repository;

public sealed record ApiExchangeSearchResult(
    IReadOnlyList<ApiExchange> Items,
    int TotalCount,
    int Page,
    int PageSize);
