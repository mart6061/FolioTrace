using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Foreign exchange rates")]
public sealed record FXRates : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public EventID LastEventID { get; private set; }

    public LastAuditDateTime LastAuditDateTime { get; private set; }

    public required List<FXRate> Items { get; init; }

    [SetsRequiredMembers]
    public FXRates(EventDateTime valuationDateTime, List<IFXEvent> fxEvents, List<IFXRateEvent> rateEvents)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, fxEvents, rateEvents), fxEvents, rateEvents)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public FXRates(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IFXEvent> fxEvents, List<IFXRateEvent> rateEvents)
    {
        var fxs = new FXs(valuationDateTime, asOfDateTime, fxEvents);
        var fxByPair = fxs.Items.ToDictionary(fx => fx.Pair);
        var latestRateEventsByPair = new Dictionary<CurrencyPair, IFXRateEvent>();
        IFXRateEvent? latestIncludedRateEvent = null;

        foreach (var rateEvent in rateEvents)
        {
            if (rateEvent.EventDateTime.Value > valuationDateTime.Value || rateEvent.AuditDateTime.Value > asOfDateTime.Value)
                continue;

            if (latestIncludedRateEvent is null || CompareEventOrder(rateEvent, latestIncludedRateEvent) > 0)
                latestIncludedRateEvent = rateEvent;

            var pair = GetPair(rateEvent);
            if (!latestRateEventsByPair.TryGetValue(pair, out var latestForPair) || CompareEventOrder(rateEvent, latestForPair) > 0)
                latestRateEventsByPair[pair] = rateEvent;
        }

        var latestRateEvents = latestRateEventsByPair.Values
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = latestIncludedRateEvent?.EventID ?? fxs.LastEventID;
        LastAuditDateTime = new LastAuditDateTime(Math.Max(
            latestIncludedRateEvent?.AuditDateTime.Value.Ticks ?? asOfDateTime.Value.Ticks,
            fxs.LastAuditDateTime.Value.Ticks) is var ticks ? new DateTime(ticks) : asOfDateTime.Value);
        Items = [];

        foreach (var item in latestRateEvents)
            Apply(item, fxByPair);

        for (var index = 0; index < Items.Count; index++)
        {
            if (fxByPair.TryGetValue(Items[index].Pair, out var fx))
            Items[index] = Items[index].Apply(fx);
        }
    }

    [SetsRequiredMembers]
    public FXRates(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime, List<FXRate> items)
    {
        ValuationDateTime = valuationDateTime ?? throw new ArgumentNullException(nameof(valuationDateTime));
        AsOfDateTime = asOfDateTime ?? throw new ArgumentNullException(nameof(asOfDateTime));
        LastEventID = lastEventID ?? throw new ArgumentNullException(nameof(lastEventID));
        LastAuditDateTime = lastAuditDateTime ?? throw new ArgumentNullException(nameof(lastAuditDateTime));
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public void Apply(FXs fxs)
    {
        for (var index = 0; index < Items.Count; index++)
        {
            var fx = fxs.Items.Single(item => item.Pair == Items[index].Pair);
            Items[index] = Items[index].Apply(fx);
        }

        LastAuditDateTime = new LastAuditDateTime(new DateTime(Math.Max(LastAuditDateTime.Value.Ticks, fxs.LastAuditDateTime.Value.Ticks)));
    }

    public void Apply(IFXRateEvent rateEvent, FXs fxs)
    {
        switch (rateEvent)
        {
            case FXRateSetEvent setEvent:
                Apply(setEvent, fxs);
                break;
            default:
                throw new InvalidOperationException($"Unsupported FX rate event type '{rateEvent.GetType().Name}'.");
        }
    }

    public void Apply(FXRateSetEvent setEvent, FXs fxs)
    {
        var fx = fxs.Items.SingleOrDefault(item => item.Pair == setEvent.Pair);
        if (fx is null)
            throw new InvalidOperationException($"No matching FX found for Pair '{setEvent.Pair}'.");

        var index = Items.FindIndex(rate => rate.Pair == setEvent.Pair);
        if (index >= 0)
            Items[index] = Items[index].Apply(setEvent);
        else
            Items.Add(FXRateBuilder.Create(fx, setEvent));

        LastEventID = setEvent.EventID;
        LastAuditDateTime = GetLastAuditDateTime();
    }

    private void Apply(IFXRateEvent rateEvent, IReadOnlyDictionary<CurrencyPair, FX> fxByPair)
    {
        switch (rateEvent)
        {
            case FXRateSetEvent setEvent:
                Apply(setEvent, fxByPair);
                break;
            default:
                throw new InvalidOperationException($"Unsupported FX rate event type '{rateEvent.GetType().Name}'.");
        }
    }

    private void Apply(FXRateSetEvent setEvent, IReadOnlyDictionary<CurrencyPair, FX> fxByPair)
    {
        if (!fxByPair.TryGetValue(setEvent.Pair, out var fx))
            throw new InvalidOperationException($"No matching FX found for Pair '{setEvent.Pair}'.");

        Items.Add(FXRateBuilder.Create(fx, setEvent));
        LastEventID = setEvent.EventID;
        LastAuditDateTime = setEvent.AuditDateTime;
    }

    private LastAuditDateTime GetLastAuditDateTime() =>
        Items.Count == 0 ? new LastAuditDateTime(AsOfDateTime.Value) : new LastAuditDateTime(Items.Max(rate => rate.LastAuditDateTime.Value));

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IFXEvent> fxEvents, List<IFXRateEvent> rateEvents)
    {
        var latest = fxEvents
            .Cast<IEventBase>()
            .Concat(rateEvents.Cast<IEventBase>())
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value)
            .Select(@event => (DateTime?)@event.AuditDateTime.Value)
            .Max();

        return latest.HasValue ? new AuditDateTime(latest.Value) : AuditDateTimeBuilder.Create();
    }

    private static CurrencyPair GetPair(IFXRateEvent rateEvent) =>
        rateEvent switch
        {
            FXRateSetEvent setEvent => setEvent.Pair,
            _ => throw new InvalidOperationException($"Unsupported FX rate event type '{rateEvent.GetType().Name}'.")
        };

    private static int CompareEventOrder(IEventBase left, IEventBase right)
    {
        var eventDateComparison = left.EventDateTime.Value.CompareTo(right.EventDateTime.Value);
        if (eventDateComparison != 0)
            return eventDateComparison;

        var auditDateComparison = left.AuditDateTime.Value.CompareTo(right.AuditDateTime.Value);
        if (auditDateComparison != 0)
            return auditDateComparison;

        return left.EventID.Value.CompareTo(right.EventID.Value);
    }
}
