using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Instruments : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<Instrument> Items { get; init; }

    [SetsRequiredMembers]
    public Instruments(EventDateTime valuationDateTime, List<IInstrumentEvent> items)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, items), items)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Instruments(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IInstrumentEvent> items)
    {
        if (valuationDateTime is null)
            throw new ArgumentNullException(nameof(valuationDateTime));

        if (asOfDateTime is null)
            throw new ArgumentNullException(nameof(asOfDateTime));

        var includedItems = (items ?? throw new ArgumentNullException(nameof(items)))
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

    public void Apply(IInstrumentEvent instrumentEvent)
    {
        switch (instrumentEvent)
        {
            case InstrumentCreatedEvent createdEvent:
                if (Items.Any(instrument => instrument.InstrumentID == createdEvent.InstrumentID))
                    throw new InvalidOperationException($"Instrument already exists for InstrumentID '{createdEvent.InstrumentID}'.");
                Items.Add(InstrumentBuilder.Create(createdEvent));
                break;
            case InstrumentModifiedEvent modifiedEvent:
                ApplyExisting(modifiedEvent.InstrumentID, instrument => instrument.Apply(modifiedEvent));
                break;
            case InstrumentActiveModifiedEvent activeModifiedEvent:
                ApplyExisting(activeModifiedEvent.InstrumentID, instrument => instrument.Apply(activeModifiedEvent));
                break;
            case InstrumentIdentifierSetEvent identifierSetEvent:
                ApplyExisting(identifierSetEvent.InstrumentID, instrument => instrument.Apply(identifierSetEvent));
                break;
            case InstrumentIdentifierUnsetEvent identifierUnsetEvent:
                ApplyExisting(identifierUnsetEvent.InstrumentID, instrument => instrument.Apply(identifierUnsetEvent));
                break;
            case InstrumentTermsSetEvent termsSetEvent:
                ApplyExisting(termsSetEvent.InstrumentID, instrument => instrument.Apply(termsSetEvent));
                break;
            default:
                throw new InvalidOperationException($"Unsupported instrument event type '{instrumentEvent.GetType().Name}'.");
        }

        LastEventID = instrumentEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime();
    }

    private void ApplyExisting(InstrumentID instrumentID, Func<Instrument, Instrument> apply)
    {
        var index = Items.FindIndex(instrument => instrument.InstrumentID == instrumentID);
        if (index < 0)
            throw new InvalidOperationException($"No matching Instrument found for InstrumentID '{instrumentID}'.");

        Items[index] = apply(Items[index]);
    }

    private LastAuditDateTime GetLastAuditDateTime() =>
        Items.Count == 0 ? new LastAuditDateTime(AsOfDateTime.Value) : new LastAuditDateTime(Items.Max(instrument => instrument.LastAuditDateTime.Value));

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IInstrumentEvent> items)
    {
        var includedItems = items
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value)
            .ToList();

        return includedItems.Count == 0
            ? AuditDateTimeBuilder.Create()
            : new AuditDateTime(includedItems.Max(@event => @event.AuditDateTime.Value));
    }
}
