using Marten;
using System.Linq;

namespace Repository;

public sealed class MartenApiExchangeRepository(IDocumentStore store) : IApiExchangeRepository
{
    public async Task StoreAsync(ApiExchange exchange, CancellationToken cancellationToken = default)
    {
        if (exchange is null)
            throw new ArgumentNullException(nameof(exchange));

        await using var session = store.LightweightSession();

        session.Store(exchange);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApiExchange?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var session = store.QuerySession();

        return await session.LoadAsync<ApiExchange>(id, cancellationToken);
    }

    public async Task<ApiExchangeSearchResult> SearchAsync(ApiExchangeSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        if (criteria is null)
            throw new ArgumentNullException(nameof(criteria));

        var page = Math.Max(1, criteria.Page);
        var pageSize = Math.Clamp(criteria.PageSize, 1, 200);

        await using var session = store.QuerySession();

        IQueryable<ApiExchange> query = session.Query<ApiExchange>();

        if (criteria.FromUtc.HasValue)
            query = query.Where(exchange => exchange.StartedAtUtc >= criteria.FromUtc.Value);

        if (criteria.ToUtc.HasValue)
            query = query.Where(exchange => exchange.StartedAtUtc <= criteria.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(criteria.Method))
        {
            var method = criteria.Method.Trim().ToUpperInvariant();
            query = query.Where(exchange => exchange.Method == method);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Path))
        {
            var path = criteria.Path.Trim();
            query = query.Where(exchange => exchange.Path.Contains(path));
        }

        if (criteria.StatusCode.HasValue)
            query = query.Where(exchange => exchange.StatusCode == criteria.StatusCode.Value);

        if (criteria.MinimumDurationMilliseconds.HasValue)
            query = query.Where(exchange => exchange.DurationMilliseconds >= criteria.MinimumDurationMilliseconds.Value);

        if (criteria.MaximumDurationMilliseconds.HasValue)
            query = query.Where(exchange => exchange.DurationMilliseconds <= criteria.MaximumDurationMilliseconds.Value);

        if (!string.IsNullOrWhiteSpace(criteria.Text))
        {
            var text = criteria.Text.Trim();
            query = query.Where(exchange =>
                (exchange.Request.Body != null && exchange.Request.Body.Contains(text)) ||
                (exchange.Response.Body != null && exchange.Response.Body.Contains(text)) ||
                (exchange.ExceptionMessage != null && exchange.ExceptionMessage.Contains(text)));
        }

        var totalCount = query.Count();
        var items = query
            .OrderByDescending(exchange => exchange.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new ApiExchangeSearchResult(items, totalCount, page, pageSize);
    }
}
