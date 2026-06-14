using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Countries : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<Country> Items { get; init; }

    [SetsRequiredMembers]
    public Countries(EventDateTime valuationDateTime, List<ICountryEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    // Regular constructor enforces rules
    [JsonConstructor]
    [SetsRequiredMembers]
    public Countries(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<ICountryEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null country events.", nameof(items));

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

    public void Apply(ICountryEvent countryEvent)
    {
        if (countryEvent is null)
            throw new ArgumentNullException(nameof(countryEvent));

        switch (countryEvent)
        {
            case CountryCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case CountryModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            case CountryFlagModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported country event type '{countryEvent.GetType().Name}'.");
        }
    }

    public void Apply(CountryCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        if (Items.Any(country => country.Alpha2 == createdEvent.Alpha2))
            throw new InvalidOperationException($"Country already exists for Alpha2 '{createdEvent.Alpha2}'.");

        Items.Add(CountryBuilder.Create(createdEvent));
        LastEventID = createdEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(CountryModifiedEvent modifiedEvent)
    {
        if (modifiedEvent is null)
            throw new ArgumentNullException(nameof(modifiedEvent));

        var index = Items.FindIndex(country => country.Alpha2 == modifiedEvent.Alpha2);
        if (index < 0)
            throw new InvalidOperationException($"No matching country found for Alpha2 '{modifiedEvent.Alpha2}'.");

        Items[index] = Items[index].Apply(modifiedEvent);
        LastEventID = modifiedEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(CountryFlagModifiedEvent modifiedEvent)
    {
        if (modifiedEvent is null)
            throw new ArgumentNullException(nameof(modifiedEvent));

        var index = Items.FindIndex(country => country.Alpha2 == modifiedEvent.Alpha2);
        if (index < 0)
            throw new InvalidOperationException($"No matching country found for Alpha2 '{modifiedEvent.Alpha2}'.");

        Items[index] = Items[index].Apply(modifiedEvent);
        LastEventID = modifiedEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    private static LastAuditDateTime GetLastAuditDateTime(List<Country> items) =>
        new LastAuditDateTime(items.Max(country => country.LastAuditDateTime.Value));

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<ICountryEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null country events.", nameof(items));

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value)
            .ToList();

        return includedItems.Any()
            ? new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value))
            : Constants.Initialisation.AuditDateTime;
    }
}
