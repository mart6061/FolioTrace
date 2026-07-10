using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Common;
using FolioTrace.Types;


namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Brokers")]
public sealed record Brokers : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<Broker> Items { get; init; }

    [SetsRequiredMembers]
    public Brokers(EventDateTime valuationDateTime, List<IBrokerEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Brokers(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IBrokerEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null broker events.", nameof(items));

        var orderedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = orderedItems.LastOrDefault()?.EventID ?? Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = orderedItems.Any()
            ? new LastAuditDateTime(orderedItems.Max(@event => @event.AuditDateTime.Value))
            : new LastAuditDateTime(asOfDateTime.Value);
        Items = [];

        foreach (var item in orderedItems)
            Apply(item);
    }

    public void Apply(IBrokerEvent brokerEvent)
    {
        if (brokerEvent is null)
            throw new ArgumentNullException(nameof(brokerEvent));

        switch (brokerEvent)
        {
            case BrokerCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case BrokerModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            case BrokerActiveSetEvent activeSetEvent:
                Apply(activeSetEvent);
                break;
            case BrokerApprovedDateTimeSetEvent approvedDateTimeSetEvent:
                Apply(approvedDateTimeSetEvent);
                break;
            case BrokerNextReviewSetEvent nextReviewSetEvent:
                Apply(nextReviewSetEvent);
                break;
            case BrokerNotesSetEvent notesSetEvent:
                Apply(notesSetEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported broker event type '{brokerEvent.GetType().Name}'.");
        }
    }

    public void Apply(BrokerCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        if (Items.Any(broker => broker.LEI == createdEvent.LEI))
            throw new InvalidOperationException($"Broker already exists for LEI '{createdEvent.LEI}'.");

        Items.Add(BrokerBuilder.Create(createdEvent));
        UpdateProvenance(createdEvent);
    }

    public void Apply(BrokerModifiedEvent modifiedEvent)
    {
        var index = FindIndex(modifiedEvent.LEI);
        Items[index] = Items[index].Apply(modifiedEvent);
        UpdateProvenance(modifiedEvent);
    }

    public void Apply(BrokerActiveSetEvent activeSetEvent)
    {
        var index = FindIndex(activeSetEvent.LEI);
        Items[index] = Items[index].Apply(activeSetEvent);
        UpdateProvenance(activeSetEvent);
    }

    public void Apply(BrokerApprovedDateTimeSetEvent approvedDateTimeSetEvent)
    {
        var index = FindIndex(approvedDateTimeSetEvent.LEI);
        Items[index] = Items[index].Apply(approvedDateTimeSetEvent);
        UpdateProvenance(approvedDateTimeSetEvent);
    }

    public void Apply(BrokerNextReviewSetEvent nextReviewSetEvent)
    {
        var index = FindIndex(nextReviewSetEvent.LEI);
        Items[index] = Items[index].Apply(nextReviewSetEvent);
        UpdateProvenance(nextReviewSetEvent);
    }

    public void Apply(BrokerNotesSetEvent notesSetEvent)
    {
        var index = FindIndex(notesSetEvent.LEI);
        Items[index] = Items[index].Apply(notesSetEvent);
        UpdateProvenance(notesSetEvent);
    }

    private int FindIndex(LegalEntityIdentifier lei)
    {
        var index = Items.FindIndex(broker => broker.LEI == lei);
        if (index < 0)
            throw new InvalidOperationException($"No matching broker found for LEI '{lei}'.");

        return index;
    }

    private void UpdateProvenance(IBrokerEvent @event)
    {
        LastEventID = @event.EventID;
        LastAuditDateTime = new LastAuditDateTime(Items.Max(broker => broker.LastAuditDateTime.Value));
    }

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IBrokerEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null broker events.", nameof(items));

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value)
            .ToList();

        return includedItems.Any()
            ? new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value))
            : Constants.Initialisation.AuditDateTime;
    }
}
