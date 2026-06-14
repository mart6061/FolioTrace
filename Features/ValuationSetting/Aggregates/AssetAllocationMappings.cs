using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationMappings : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<AssetAllocationMapping> Items { get; init; }

    [SetsRequiredMembers]
    public AssetAllocationMappings(EventDateTime valuationDateTime, List<IAssetAllocationMappingEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public AssetAllocationMappings(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IAssetAllocationMappingEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null asset allocation mapping events.", nameof(items));

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;

        var orderedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        if (!orderedItems.Any())
        {
            LastEventID = Constants.Initialisation.EmptyViewEventID;
            LastAuditDateTime = new LastAuditDateTime(DateTime.MinValue.AddTicks(1));
            Items = [];
            return;
        }

        LastEventID = orderedItems.Last().EventID;
        LastAuditDateTime = new LastAuditDateTime(orderedItems.Max(@event => @event.AuditDateTime.Value));
        Items = orderedItems
            .GroupBy(@event => new { AssetAllocationID = @event.AssetAllocationID.Value, HoldingID = @event.HoldingID.Value })
            .Select(group => group.Last())
            .OrderBy(@event => @event.AssetAllocationID.Value)
            .ThenBy(@event => @event.HoldingID.Value)
            .Select(@event => new AssetAllocationMapping(
                @event.AssetAllocationID,
                @event.HoldingID,
                @event.NodeID,
                @event.EventDateTime,
                @event.AuditDateTime,
                @event.EventID,
                new LastAuditDateTime(@event.AuditDateTime.Value)))
            .ToList();
    }

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IAssetAllocationMappingEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value)
            .ToList();

        if (!includedItems.Any())
            return Constants.Initialisation.AuditDateTime;

        return new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value));
    }
}
