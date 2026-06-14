using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportConfigs : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<ReportConfig> Items { get; init; }

    [SetsRequiredMembers]
    public ReportConfigs(EventDateTime valuationDateTime, List<IReportEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public ReportConfigs(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IReportEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null report events.", nameof(items));

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .ToList();

        if (!includedItems.Any())
        {
            ValuationDateTime = valuationDateTime;
            AsOfDateTime = asOfDateTime;
            LastEventID = Constants.Initialisation.EmptyViewEventID;
            LastAuditDateTime = new LastAuditDateTime(DateTime.MinValue.AddTicks(1));
            Items = [];
            return;
        }

        var orderedItems = includedItems
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = orderedItems.Last().EventID;
        LastAuditDateTime = new LastAuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value));
        Items = [];

        foreach (var item in orderedItems)
            Apply(item);
    }

    public void Apply(IReportEvent reportEvent)
    {
        if (reportEvent is null)
            throw new ArgumentNullException(nameof(reportEvent));

        switch (reportEvent)
        {
            case ReportCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case ReportModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported report event type '{reportEvent.GetType().Name}'.");
        }
    }

    public void Apply(ReportCreatedEvent createdEvent)
    {
        if (Items.Any(item => item.ReportID == createdEvent.ReportID))
            throw new InvalidOperationException($"Report already exists for ReportID '{createdEvent.ReportID}'.");

        Items.Add(ReportConfigBuilder.Create(createdEvent));
        LastEventID = createdEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(ReportModifiedEvent modifiedEvent)
    {
        var index = FindIndex(modifiedEvent.ReportID);
        Items[index] = ReportConfigBuilder.Apply(Items[index], modifiedEvent);
        LastEventID = modifiedEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    private int FindIndex(ReportID reportID)
    {
        var index = Items.FindIndex(item => item.ReportID == reportID);
        if (index < 0)
            throw new InvalidOperationException($"No matching report found for ReportID '{reportID}'.");

        return index;
    }

    private static LastAuditDateTime GetLastAuditDateTime(List<ReportConfig> items) =>
        new(items.Max(item => item.LastAuditDateTime.Value));

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IReportEvent> items)
    {
        var includedItems = items.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        if (!includedItems.Any())
            return Constants.Initialisation.AuditDateTime;

        return new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value));
    }
}
