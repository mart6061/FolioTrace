using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Currencies : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<Currency> Items { get; init; }

    [SetsRequiredMembers]
    public Currencies(EventDateTime valuationDateTime, List<ICurrencyEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    // Regular constructor enforces rules
    [JsonConstructor]
    [SetsRequiredMembers]
    public Currencies(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<ICurrencyEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null currency events.", nameof(items));

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

    public void Apply(ICurrencyEvent currencyEvent)
    {
        if (currencyEvent is null)
            throw new ArgumentNullException(nameof(currencyEvent));

        switch (currencyEvent)
        {
            case CurrencyCreatedEvent createdEvent:
                Apply(createdEvent);
                break;
            case CurrencyModifiedEvent modifiedEvent:
                Apply(modifiedEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported currency event type '{currencyEvent.GetType().Name}'.");
        }
    }

    public void Apply(CurrencyCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        if (Items.Any(currency => currency.AlphabeticCode == createdEvent.AlphabeticCode))
            throw new InvalidOperationException($"Currency already exists for AlphabeticCode '{createdEvent.AlphabeticCode}'.");

        Items.Add(CurrencyBuilder.Create(createdEvent));
        LastEventID = createdEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    public void Apply(CurrencyModifiedEvent modifiedEvent)
    {
        if (modifiedEvent is null)
            throw new ArgumentNullException(nameof(modifiedEvent));

        var index = Items.FindIndex(currency => currency.AlphabeticCode == modifiedEvent.AlphabeticCode);
        if (index < 0)
            throw new InvalidOperationException($"No matching currency found for AlphabeticCode '{modifiedEvent.AlphabeticCode}'.");

        Items[index] = Items[index].Apply(modifiedEvent);
        LastEventID = modifiedEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime(Items);
    }

    private static LastAuditDateTime GetLastAuditDateTime(List<Currency> items) =>
        new LastAuditDateTime(items.Max(currency => currency.LastAuditDateTime.Value));

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<ICurrencyEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (items.Any(@event => @event is null))
            throw new ArgumentException("Value must not contain null currency events.", nameof(items));

        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value)
            .ToList();

        return includedItems.Any()
            ? new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value))
            : Constants.Initialisation.AuditDateTime;
    }
}
