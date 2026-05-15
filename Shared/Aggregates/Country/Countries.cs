using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AILibrary.Types;

namespace AILibrary.Aggregates;

public sealed record Countries : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<Country> Items { get; init; }

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

        if (!items.Any())
            throw new ArgumentException("Value must contain at least one country event.", nameof(items));

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .ToList();

        if (!includedItems.Any())
            throw new ArgumentException("Value must contain at least one country event within the valuation and as-of date time.", nameof(items));

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastAuditDateTime = new LastAuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value));
        Items = [];

        foreach (var item in includedItems.OfType<CountryCreatedEvent>().OrderBy(@event => @event.EventDateTime))
            Apply(item);

        foreach (var item in includedItems.OfType<CountryModifiedEvent>().OrderBy(@event => @event.EventDateTime))
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
            default:
                throw new InvalidOperationException($"Unsupported country event type '{countryEvent.GetType().Name}'.");
        }
    }

    public void Apply(CountryCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        if (Items.Any(country => country.ISO2 == createdEvent.ISO2))
            throw new InvalidOperationException($"Country already exists for ISO2 '{createdEvent.ISO2}'.");

        Items.Add(CountryBuilder.Create(createdEvent));
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(CountryModifiedEvent modifiedEvent)
    {
        if (modifiedEvent is null)
            throw new ArgumentNullException(nameof(modifiedEvent));

        var index = Items.FindIndex(country => country.ISO2 == modifiedEvent.ISO2);
        if (index < 0)
            throw new InvalidOperationException($"No matching country found for ISO2 '{modifiedEvent.ISO2}'.");

        Items[index] = Items[index].Apply(modifiedEvent);
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public string ToData() => $"{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Countries)}: (ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()}, Items: {Items.Count})";

    private static LastAuditDateTime GetLastAuditDateTime(List<Country> items) =>
        new LastAuditDateTime(items.Max(country => country.LastAuditDateTime.Value));
}
