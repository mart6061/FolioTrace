using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;


namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Foreign exchange pairs")]
public sealed record FXs : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<FX> Items { get; init; }

    [SetsRequiredMembers]
    public FXs(EventDateTime valuationDateTime, List<IFXEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public FXs(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IFXEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = includedItems.LastOrDefault()?.EventID ?? Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = new LastAuditDateTime(includedItems.LastOrDefault()?.AuditDateTime.Value ?? asOfDateTime.Value);
        Items = [];

        foreach (var item in includedItems)
            Apply(item);
    }

    public void Apply(IFXEvent fxEvent)
    {
        switch (fxEvent)
        {
            case FXCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case FXActiveModifiedEvent activeModifiedEvent:
                Apply(activeModifiedEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported FX event type '{fxEvent.GetType().Name}'.");
        }
    }

    public void Apply(FXCreatedEvent createdEvent)
    {
        if (Items.Any(fx => fx.Pair == createdEvent.Pair))
            throw new InvalidOperationException($"FX already exists for Pair '{createdEvent.Pair}'.");

        Items.Add(FXBuilder.Create(createdEvent));
        LastEventID = createdEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime();
    }

    public void Apply(FXActiveModifiedEvent activeModifiedEvent)
    {
        var index = Items.FindIndex(fx => fx.Pair == activeModifiedEvent.Pair);
        if (index < 0)
            throw new InvalidOperationException($"No matching FX found for Pair '{activeModifiedEvent.Pair}'.");

        Items[index] = Items[index].Apply(activeModifiedEvent);
        LastEventID = activeModifiedEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime();
    }

    private LastAuditDateTime GetLastAuditDateTime() =>
        Items.Count == 0 ? new LastAuditDateTime(AsOfDateTime.Value) : new LastAuditDateTime(Items.Max(fx => fx.LastAuditDateTime.Value));

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IFXEvent> items)
    {
        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value)
            .ToList();

        return includedItems.Count == 0
            ? AuditDateTimeBuilder.Create()
            : new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value));
    }
}
