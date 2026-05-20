namespace Repository;

public interface IApiExchangeRepository
{
    Task StoreAsync(ApiExchange exchange, CancellationToken cancellationToken = default);

    Task<ApiExchange?> LoadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiExchangeSearchResult> SearchAsync(ApiExchangeSearchCriteria criteria, CancellationToken cancellationToken = default);
}
