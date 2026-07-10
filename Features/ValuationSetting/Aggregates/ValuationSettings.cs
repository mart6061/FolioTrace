using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Common;
using FolioTrace.Types;


namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Valuation settings")]
public sealed record ValuationSettings : IAggregate
{
    [JsonIgnore]
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<ValuationSetting> Items { get; init; }

    [SetsRequiredMembers]
    public ValuationSettings(EventDateTime valuationDateTime, List<IValuationSettingEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public ValuationSettings(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IValuationSettingEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null valuation setting events.", nameof(items));

        var includedItems = items
            .Where(@event => @event.AuditDateTime.Value <= asOfDateTime.Value)
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
            .OrderBy(@event => @event.AuditDateTime.Value)
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

    public void Apply(IValuationSettingEvent valuationSettingEvent)
    {
        if (valuationSettingEvent is null)
            throw new ArgumentNullException(nameof(valuationSettingEvent));

        switch (valuationSettingEvent)
        {
            case AssetAllocationCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case AssetAllocationModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            case AssetAllocationAccountIDsSetEvent accountIDsSetEvent:
                Apply(accountIDsSetEvent);
                break;
            case AssetAllocationActiveSetEvent activeSetEvent:
                Apply(activeSetEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported valuation setting event type '{valuationSettingEvent.GetType().Name}'.");
        }
    }

    public void Apply(AssetAllocationCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        if (Items.Any(item => item.AssetAllocationID == createdEvent.AssetAllocationID))
            throw new InvalidOperationException($"Asset allocation already exists for AssetAllocationID '{createdEvent.AssetAllocationID}'.");

        Items.Add(ValuationSettingBuilder.Create(createdEvent));
        LastEventID = createdEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(AssetAllocationModifiedEvent modifiedEvent)
    {
        var index = FindIndex(modifiedEvent.AssetAllocationID);
        Items[index] = ValuationSettingBuilder.Apply(Items[index], modifiedEvent);
        LastEventID = modifiedEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(AssetAllocationAccountIDsSetEvent accountIDsSetEvent)
    {
        var index = FindIndex(accountIDsSetEvent.AssetAllocationID);
        Items[index] = ValuationSettingBuilder.Apply(Items[index], accountIDsSetEvent);
        LastEventID = accountIDsSetEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(AssetAllocationActiveSetEvent activeSetEvent)
    {
        var index = FindIndex(activeSetEvent.AssetAllocationID);
        Items[index] = ValuationSettingBuilder.Apply(Items[index], activeSetEvent);
        LastEventID = activeSetEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    private int FindIndex(AssetAllocationID assetAllocationID)
    {
        var index = Items.FindIndex(item => item.AssetAllocationID == assetAllocationID);
        if (index < 0)
            throw new InvalidOperationException($"No matching asset allocation found for AssetAllocationID '{assetAllocationID}'.");

        return index;
    }

    private static LastAuditDateTime GetLastAuditDateTime(List<ValuationSetting> items) =>
        new(items.Max(item => item.LastAuditDateTime.Value));

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IValuationSettingEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null valuation setting events.", nameof(items));

        var includedItems = items.ToList();

        if (!includedItems.Any())
            return Constants.Initialisation.AuditDateTime;

        return new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value));
    }
}
