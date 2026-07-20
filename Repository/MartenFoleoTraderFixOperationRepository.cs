using FolioTrace.Aggregates;
using Marten;
using Marten.Linq;

namespace Repository;

public sealed class MartenFoleoTraderFixOperationRepository(IDocumentStore store) : IFoleoTraderFixOperationRepository
{
    private static readonly IReadOnlyDictionary<string, string> MessageNames = new Dictionary<string, string>
    {
        ["0"] = "Heartbeat",
        ["1"] = "Test Request",
        ["2"] = "Resend Request",
        ["3"] = "Reject",
        ["4"] = "Sequence Reset",
        ["5"] = "Logout",
        ["8"] = "Execution Report",
        ["9"] = "Order Cancel Reject",
        ["A"] = "Logon",
        ["D"] = "New Order Single",
        ["F"] = "Order Cancel Request",
        ["G"] = "Order Cancel Replace Request"
    };

    public async Task<FoleoTraderFixOperationSearchResult> SearchAsync(
        FoleoTraderFixOperationSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var page = Math.Max(1, criteria.Page);
        var pageSize = Math.Clamp(criteria.PageSize, 1, 200);
        await using var session = store.QuerySession();
        IQueryable<FoleoTraderFIXOperationRecordedEvent> query = session.Events.QueryRawEventDataOnly<FoleoTraderFIXOperationRecordedEvent>();

        if (criteria.FromUtc.HasValue)
            query = query.Where(item => item.AuditDateTime.Value >= criteria.FromUtc.Value);
        if (criteria.ToUtc.HasValue)
            query = query.Where(item => item.AuditDateTime.Value <= criteria.ToUtc.Value);

        query = ApplyExactFilter(query, criteria.Direction, item => item.Direction);
        query = ApplyExactFilter(query, criteria.Channel, item => item.Channel);
        query = ApplyExactFilter(query, criteria.MsgType, item => item.MsgType);
        query = ApplyContainsFilter(query, criteria.ClOrdID, item => item.ClOrdID);
        query = ApplyContainsFilter(query, criteria.ExecID, item => item.ExecID);

        if (!string.IsNullOrWhiteSpace(criteria.Text))
        {
            var text = criteria.Text.Trim().ToLowerInvariant();
            var matchingMessageTypes = MessageNames
                .Where(entry => entry.Value.Contains(criteria.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.Key)
                .ToArray();
            query = query.Where(item =>
                item.RawMessage.ToLower().Contains(text) ||
                item.SessionID.ToLower().Contains(text) ||
                item.Direction.ToLower().Contains(text) ||
                item.Channel.ToLower().Contains(text) ||
                item.MsgType.ToLower().Contains(text) ||
                item.SenderCompID.ToLower().Contains(text) ||
                item.TargetCompID.ToLower().Contains(text) ||
                item.ClOrdID.ToLower().Contains(text) ||
                item.ExecID.ToLower().Contains(text) ||
                matchingMessageTypes.Contains(item.MsgType));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.AuditDateTime.Value)
            .ThenByDescending(item => item.EventID.Value)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new FoleoTraderFixOperationSearchResult(items, totalCount, page, pageSize);
    }

    private static IQueryable<FoleoTraderFIXOperationRecordedEvent> ApplyExactFilter(
        IQueryable<FoleoTraderFIXOperationRecordedEvent> query,
        string? filter,
        System.Linq.Expressions.Expression<Func<FoleoTraderFIXOperationRecordedEvent, string>> selector)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return query;

        var value = filter.Trim().ToLowerInvariant();
        return query.Where(BuildStringPredicate(selector, value, exact: true));
    }

    private static IQueryable<FoleoTraderFIXOperationRecordedEvent> ApplyContainsFilter(
        IQueryable<FoleoTraderFIXOperationRecordedEvent> query,
        string? filter,
        System.Linq.Expressions.Expression<Func<FoleoTraderFIXOperationRecordedEvent, string>> selector)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return query;

        return query.Where(BuildStringPredicate(selector, filter.Trim().ToLowerInvariant(), exact: false));
    }

    private static System.Linq.Expressions.Expression<Func<FoleoTraderFIXOperationRecordedEvent, bool>> BuildStringPredicate(
        System.Linq.Expressions.Expression<Func<FoleoTraderFIXOperationRecordedEvent, string>> selector,
        string value,
        bool exact)
    {
        var lowered = System.Linq.Expressions.Expression.Call(selector.Body, nameof(string.ToLower), Type.EmptyTypes);
        System.Linq.Expressions.Expression comparison = exact
            ? System.Linq.Expressions.Expression.Equal(lowered, System.Linq.Expressions.Expression.Constant(value))
            : System.Linq.Expressions.Expression.Call(lowered, nameof(string.Contains), Type.EmptyTypes, System.Linq.Expressions.Expression.Constant(value));
        return System.Linq.Expressions.Expression.Lambda<Func<FoleoTraderFIXOperationRecordedEvent, bool>>(comparison, selector.Parameters);
    }
}
