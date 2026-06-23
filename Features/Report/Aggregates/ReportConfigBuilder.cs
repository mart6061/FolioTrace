using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class ReportConfigBuilder
{
    private static readonly ReportValuationColumnKey[] DefaultValuationColumnKeys =
    [
        ReportValuationColumnKey.InstrumentName,
        ReportValuationColumnKey.ISIN,
        ReportValuationColumnKey.Sedol,
        ReportValuationColumnKey.QuotePrice,
        ReportValuationColumnKey.Quantity,
        ReportValuationColumnKey.BookValue,
        ReportValuationColumnKey.BookValueDefault,
        ReportValuationColumnKey.BookValueFIFO,
        ReportValuationColumnKey.BookValueLIFO,
        ReportValuationColumnKey.BookValueRunningAverage,
        ReportValuationColumnKey.BookCost,
        ReportValuationColumnKey.Weight,
        ReportValuationColumnKey.Target,
        ReportValuationColumnKey.Min,
        ReportValuationColumnKey.Max
    ];

    public static ReportConfig Create(ReportCreatedEvent createdEvent) =>
        new(
            createdEvent.ReportID,
            createdEvent.Name,
            createdEvent.Active,
            CloneNodes(createdEvent.Nodes),
            createdEvent.AuditDateTime,
            createdEvent.EventID);

    public static ReportConfig Apply(ReportConfig report, ReportModifiedEvent modifiedEvent) =>
        report with
        {
            Name = modifiedEvent.Name,
            Active = modifiedEvent.Active,
            Nodes = CloneNodes(modifiedEvent.Nodes),
            LastEventID = modifiedEvent.EventID,
            LastAuditDateTime = new LastAuditDateTime(modifiedEvent.AuditDateTime.Value)
        };

    public static EventDateTime DateOnly(EventDateTime eventDateTime)
    {
        if (eventDateTime is null)
            throw new ArgumentNullException(nameof(eventDateTime));

        return new EventDateTime(eventDateTime.Value.Date);
    }

    public static List<ReportNodeBase> CloneNodes(IEnumerable<ReportNodeBase>? nodes) =>
        nodes?.Select(CloneNode).OrderBy(node => node.DisplayOrder).ToList() ?? [];

    public static List<ReportNodeBase> NormaliseNodes(IEnumerable<ReportNodeBase>? nodes) =>
        CloneNodes(nodes)
            .Select((node, index) => node with { DisplayOrder = index + 1 })
            .ToList();

    private static ReportNodeBase CloneNode(ReportNodeBase node) =>
        node switch
        {
            ReportNodeCoverPage coverPage => coverPage with { Name = Clean(coverPage.Name), Title = Clean(coverPage.Title) },
            ReportNodeIndex index => index with { Name = Clean(index.Name), Title = Clean(index.Title) },
            ReportNodeChart chart => chart with { Name = Clean(chart.Name), Title = Clean(chart.Title) },
            ReportNodeValuation valuation => valuation with
            {
                Name = Clean(valuation.Name),
                Title = Clean(valuation.Title),
                Columns = NormaliseValuationColumns(valuation.Columns)
            },
            ReportNodeTransactions transactions => transactions with { Name = Clean(transactions.Name), Title = Clean(transactions.Title) },
            ReportNodeCash cash => cash with { Name = Clean(cash.Name), Title = Clean(cash.Title) },
            _ => throw new InvalidOperationException($"Unsupported report node type '{node.GetType().Name}'.")
        };

    public static List<ReportValuationColumn> NormaliseValuationColumns(IEnumerable<ReportValuationColumn>? columns) =>
        columns is null
            ? DefaultValuationColumns()
            : columns
                .OrderBy(column => column.DisplayOrder)
                .Select((column, index) => column with { DisplayOrder = index + 1 })
                .ToList();

    public static List<ReportValuationColumn> DefaultValuationColumns() =>
        DefaultValuationColumnKeys
            .Select((columnKey, index) => new ReportValuationColumn(columnKey, index + 1))
            .ToList();

    private static string Clean(string value) => value?.Trim() ?? string.Empty;
}
