using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Holdings : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<HoldingBase> Items { get; init; }

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

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .ToList();
        if (!includedItems.Any())
        {
            ValuationDateTime = valuationDateTime;
            AsOfDateTime = asOfDateTime;
            LastEventID = Constants.Initialisation.EmptyViewEventID;
            LastAuditDateTime = new LastAuditDateTime(asOfDateTime.Value);
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
        if (createdEvent.Default && Items.Any(holding => HasDefaultConflict(holding, createdEvent.AccountID, createdEvent.InstrumentID, createdKind)))
            throw new InvalidOperationException(DefaultConflictMessage(createdKind, createdEvent.AccountID, createdEvent.InstrumentID));

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
        if (modifiedEvent.Default && Items.Where((_, itemIndex) => itemIndex != index).Any(holding => HasDefaultConflict(holding, existing.AccountID, existing.InstrumentID, modifiedKind)))
            throw new InvalidOperationException(DefaultConflictMessage(modifiedKind, existing.AccountID, existing.InstrumentID));

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

    private static LastAuditDateTime GetLastAuditDateTime(List<HoldingBase> items) =>
        new(items.Max(holding => holding.LastAuditDateTime.Value));

    private static bool HasDefaultConflict(HoldingBase holding, AccountID accountID, InstrumentID instrumentID, string holdingKind) =>
        holding.AccountID == accountID &&
        holding.GetHoldingKindName() == holdingKind &&
        holding.Default &&
        (HoldingKindRuntime.IsNominalKindName(holdingKind) || holding.InstrumentID == instrumentID);

    private static string DefaultConflictMessage(string holdingKind, AccountID accountID, InstrumentID instrumentID) =>
        HoldingKindRuntime.IsNominalKindName(holdingKind)
            ? $"A default {holdingKind} holding already exists for AccountID '{accountID}'."
            : $"A default {holdingKind} holding already exists for AccountID '{accountID}' and InstrumentID '{instrumentID}'.";

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IHoldingEvent> items)
    {
        var includedItems = items.Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value).ToList();
        return includedItems.Any()
            ? new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value))
            : Constants.Initialisation.AuditDateTime;
    }
}
