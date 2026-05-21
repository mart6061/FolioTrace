using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

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
        var includedRateEvents = rateEvents
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value && @event.AuditDateTime.Value <= asOfDateTime.Value)
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();
        var latestRateEvents = includedRateEvents
            .GroupBy(GetPair)
            .Select(group => group.Last())
            .OrderBy(@event => @event.EventDateTime.Value)
            .ThenBy(@event => @event.AuditDateTime.Value)
            .ThenBy(@event => @event.EventID.Value)
            .ToList();

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = includedRateEvents.LastOrDefault()?.EventID ?? fxs.LastEventID;
        LastAuditDateTime = new LastAuditDateTime(Math.Max(
            includedRateEvents.LastOrDefault()?.AuditDateTime.Value.Ticks ?? asOfDateTime.Value.Ticks,
            fxs.LastAuditDateTime.Value.Ticks) is var ticks ? new DateTime(ticks) : asOfDateTime.Value);
        Items = [];

        foreach (var item in latestRateEvents)
            Apply(item, fxs);

        foreach (var fx in fxs.Items)
        {
            var index = Items.FindIndex(rate => rate.Pair == fx.Pair);
            if (index >= 0)
                Items[index] = Items[index].Apply(fx);
        }
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

    public string ToData() => $"{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(FXRates)}: (ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastEventID: {LastEventID.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()}, Items: {Items.Count})";

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
}
