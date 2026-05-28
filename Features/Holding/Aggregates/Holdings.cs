using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Holdings : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<Holding> Items { get; init; }

    [SetsRequiredMembers]
    public Holdings(EventDateTime valuationDateTime, List<IHoldingEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Holdings(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IHoldingEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));
        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));
        if (items is null)
            throw new ArgumentNullException(nameof(items));
        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null holding events.", nameof(items));
        if (!items.Any())
            throw new ArgumentException("Value must contain at least one holding event.", nameof(items));

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .ToList();
        if (!includedItems.Any())
            throw new ArgumentException("Value must contain at least one holding event within the valuation and as-of date time.", nameof(items));

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

    public void Apply(IHoldingEvent holdingEvent)
    {
        switch (holdingEvent)
        {
            case HoldingCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case HoldingModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            case HoldingActiveModifiedEvent activeModifiedEvent:
                Apply(activeModifiedEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported holding event type '{holdingEvent.GetType().Name}'.");
        }
    }

    public void Apply(HoldingCreatedEvent createdEvent)
    {
        if (Items.Any(holding => holding.HoldingID == createdEvent.HoldingID))
            throw new InvalidOperationException($"Holding already exists for HoldingID '{createdEvent.HoldingID}'.");
        var createdKind = createdEvent.GetHoldingKindName();
        if (createdEvent.Default && Items.Any(holding => holding.AccountID == createdEvent.AccountID && holding.InstrumentID == createdEvent.InstrumentID && holding.GetHoldingKindName() == createdKind && holding.Default))
            throw new InvalidOperationException($"A default {createdKind} holding already exists for AccountID '{createdEvent.AccountID}' and InstrumentID '{createdEvent.InstrumentID}'.");

        Items.Add(HoldingBuilder.Create(createdEvent));
        LastEventID = createdEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(HoldingModifiedEvent modifiedEvent)
    {
        var index = Items.FindIndex(holding => holding.HoldingID == modifiedEvent.HoldingID);
        if (index < 0)
            throw new InvalidOperationException($"No matching holding found for HoldingID '{modifiedEvent.HoldingID}'.");

        var existing = Items[index];
        var modifiedKind = modifiedEvent.GetHoldingKindName();
        if (modifiedEvent.Default && Items.Where((_, itemIndex) => itemIndex != index).Any(holding => holding.AccountID == existing.AccountID && holding.InstrumentID == existing.InstrumentID && holding.GetHoldingKindName() == modifiedKind && holding.Default))
            throw new InvalidOperationException($"A default {modifiedKind} holding already exists for AccountID '{existing.AccountID}' and InstrumentID '{existing.InstrumentID}'.");

        Items[index] = existing.Apply(modifiedEvent);
        LastEventID = modifiedEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(HoldingActiveModifiedEvent activeModifiedEvent)
    {
        var index = Items.FindIndex(holding => holding.HoldingID == activeModifiedEvent.HoldingID);
        if (index < 0)
            throw new InvalidOperationException($"No matching holding found for HoldingID '{activeModifiedEvent.HoldingID}'.");

        Items[index] = Items[index].Apply(activeModifiedEvent);
        LastEventID = activeModifiedEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public string ToData() => $"{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Holdings)}: (ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastEventID: {LastEventID.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()}, Items: {Items.Count})";

    private static LastAuditDateTime GetLastAuditDateTime(List<Holding> items) =>
        new(items.Max(holding => holding.LastAuditDateTime.Value));

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IHoldingEvent> items)
    {
        var includedItems = items.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        if (!includedItems.Any())
            throw new ArgumentException("Value must contain at least one holding event within the valuation date time.", nameof(items));
        return new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value));
    }
}
