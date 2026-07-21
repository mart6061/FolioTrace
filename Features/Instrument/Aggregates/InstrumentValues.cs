using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;


namespace FolioTrace.Aggregates;

[FeatureAggregate(Description = "Instrument prices and income")]
public sealed record InstrumentValues : IAggregate
{
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public EventID LastEventID { get; private set; }
    public LastAuditDateTime LastAuditDateTime { get; private set; }
    public required List<InstrumentValue> Items { get; init; }

    [SetsRequiredMembers]
    public InstrumentValues(EventDateTime valuationDateTime, List<IInstrumentEvent> instrumentEvents, List<IInstrumentPriceEvent> priceEvents, List<IInstrumentIncomeEvent> incomeEvents)
        : this(valuationDateTime, GetLatestAuditDateTime(valuationDateTime, instrumentEvents, priceEvents, incomeEvents), instrumentEvents, priceEvents, incomeEvents)
    {
    }

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentValues(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, List<IInstrumentEvent> instrumentEvents, List<IInstrumentPriceEvent> priceEvents, List<IInstrumentIncomeEvent> incomeEvents)
    {
        var instruments = new Instruments(valuationDateTime, asOfDateTime, instrumentEvents);
        var latestPriceByInstrument = LatestByInstrument(priceEvents, valuationDateTime, asOfDateTime);
        var latestIncomeByInstrument = LatestByInstrument(incomeEvents, valuationDateTime, asOfDateTime);
        var valueEvents = latestPriceByInstrument.Values.Cast<IEventBase>().Concat(latestIncomeByInstrument.Values.Cast<IEventBase>()).ToList();
        var latestEvent = instruments.Items.Count == 0 && valueEvents.Count == 0
            ? null
            : valueEvents.Append(new EmptyMarkerEvent(instruments.LastEventID, instruments.LastAuditDateTime.Value, valuationDateTime, asOfDateTime)).OrderBy(@event => @event.EventDateTime.Value).ThenBy(@event => @event.AuditDateTime.Value).ThenBy(@event => @event.EventID.Value).Last();
        var latestAuditDateTime = valueEvents.Select(@event => (DateTime?)@event.AuditDateTime.Value).Append(instruments.LastAuditDateTime.Value).Max() ?? asOfDateTime.Value;

        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = latestEvent?.EventID ?? Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = new LastAuditDateTime(latestAuditDateTime);
        Items = [];

        foreach (var instrument in instruments.Items.OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            latestPriceByInstrument.TryGetValue(instrument.InstrumentID, out var priceEvent);
            latestIncomeByInstrument.TryGetValue(instrument.InstrumentID, out var incomeEvent);
            var valuePair = CreateValuePair(priceEvent, incomeEvent);
            var lastValueEvent = new[] { priceEvent as IEventBase, incomeEvent as IEventBase }
                .Where(@event => @event is not null)
                .OrderBy(@event => @event!.EventDateTime.Value)
                .ThenBy(@event => @event!.AuditDateTime.Value)
                .ThenBy(@event => @event!.EventID.Value)
                .LastOrDefault();

            var lastAudit = new[] { instrument.LastAuditDateTime.Value, priceEvent?.AuditDateTime.Value, incomeEvent?.AuditDateTime.Value }
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .Max();

            Items.Add(new InstrumentValue(
                instrument,
                valuePair.Price,
                valuePair.PriceValuationDateTime,
                valuePair.Income,
                lastValueEvent?.EventDateTime ?? instrument.ValuationDateTime,
                lastValueEvent?.AuditDateTime ?? instrument.AsOfDateTime,
                lastValueEvent?.EventID ?? instrument.LastEventID,
                new LastAuditDateTime(lastAudit)));
        }
    }

    [SetsRequiredMembers]
    public InstrumentValues(EventDateTime valuationDateTime, AuditDateTime asOfDateTime, Instruments instruments, List<InstrumentValue> baselineItems, IEnumerable<IInstrumentPriceEvent> priceDeltaEvents, IEnumerable<IInstrumentIncomeEvent> incomeDeltaEvents)
    {
        ValuationDateTime = valuationDateTime ?? throw new ArgumentNullException(nameof(valuationDateTime));
        AsOfDateTime = asOfDateTime ?? throw new ArgumentNullException(nameof(asOfDateTime));
        var baseline = (baselineItems ?? throw new ArgumentNullException(nameof(baselineItems))).ToDictionary(item => item.InstrumentID);
        var prices = LatestByInstrument(priceDeltaEvents ?? throw new ArgumentNullException(nameof(priceDeltaEvents)), valuationDateTime, asOfDateTime);
        var incomes = LatestByInstrument(incomeDeltaEvents ?? throw new ArgumentNullException(nameof(incomeDeltaEvents)), valuationDateTime, asOfDateTime);
        Items = [];

        foreach (var instrument in instruments.Items.OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            baseline.TryGetValue(instrument.InstrumentID, out var prior);
            prices.TryGetValue(instrument.InstrumentID, out var priceDelta);
            incomes.TryGetValue(instrument.InstrumentID, out var incomeDelta);
            var priceEvent = priceDelta as InstrumentPriceSetEvent;
            var incomeEvent = incomeDelta as InstrumentIncomeSetEvent;
            var price = priceEvent?.Price ?? prior?.Price;
            var income = incomeEvent?.Income ?? prior?.Income;
            var priceDate = priceEvent?.EventDateTime ?? prior?.PriceValuationDateTime;
            if (price is null || income is null || !IsValidValuePair(price, income))
            {
                price = null;
                income = null;
                priceDate = null;
            }

            var latest = new (DateTime EventDate, DateTime AuditDate, Guid EventID)[]
            {
                (instrument.ValuationDateTime.Value, instrument.AsOfDateTime.Value, instrument.LastEventID.Value),
                (prior?.ValuationDateTime.Value ?? DateTime.MinValue, prior?.AsOfDateTime.Value ?? DateTime.MinValue, prior?.LastEventID.Value ?? Guid.Empty),
                (priceEvent?.EventDateTime.Value ?? DateTime.MinValue, priceEvent?.AuditDateTime.Value ?? DateTime.MinValue, priceEvent?.EventID.Value ?? Guid.Empty),
                (incomeEvent?.EventDateTime.Value ?? DateTime.MinValue, incomeEvent?.AuditDateTime.Value ?? DateTime.MinValue, incomeEvent?.EventID.Value ?? Guid.Empty)
            }.OrderBy(item => item.EventDate).ThenBy(item => item.AuditDate).ThenBy(item => item.EventID).Last();

            Items.Add(new InstrumentValue(instrument, price, priceDate, income,
                new EventDateTime(latest.EventDate), new AuditDateTime(latest.AuditDate), new EventID(latest.EventID),
                new LastAuditDateTime(new[] { instrument.LastAuditDateTime.Value, prior?.LastAuditDateTime.Value, priceEvent?.AuditDateTime.Value, incomeEvent?.AuditDateTime.Value }.Where(value => value.HasValue).Max()!.Value)));
        }

        var aggregateLatest = Items.OrderBy(item => item.ValuationDateTime.Value).ThenBy(item => item.AsOfDateTime.Value).ThenBy(item => item.LastEventID.Value).LastOrDefault();
        LastEventID = aggregateLatest?.LastEventID ?? Constants.Initialisation.EmptyViewEventID;
        LastAuditDateTime = Items.Count == 0
            ? new LastAuditDateTime(asOfDateTime.Value)
            : new LastAuditDateTime(Items.Max(item => item.LastAuditDateTime.Value));
    }

    private static InstrumentValuePair CreateValuePair(IInstrumentPriceEvent? priceEvent, IInstrumentIncomeEvent? incomeEvent)
    {
        if (priceEvent is not InstrumentPriceSetEvent priceSetEvent || incomeEvent is not InstrumentIncomeSetEvent incomeSetEvent)
            return InstrumentValuePair.Empty;

        if (!IsValidValuePair(priceSetEvent.Price, incomeSetEvent.Income))
            return InstrumentValuePair.Empty;

        return new InstrumentValuePair(priceSetEvent.Price, priceSetEvent.EventDateTime, incomeSetEvent.Income);
    }

    private static bool IsValidValuePair(IInstrumentPrice price, IInstrumentIncome income) =>
        (price, income) switch
        {
            (InstrumentPriceCash, InstrumentIncomeCash) => true,
            (InstrumentPriceFixedIncome, InstrumentIncomeFixedIncome) => true,
            (InstrumentPriceEquity, InstrumentIncomeEquity) => true,
            _ => false
        };

    private static Dictionary<InstrumentID, TEvent> LatestByInstrument<TEvent>(IEnumerable<TEvent> events, EventDateTime valuationDateTime, AuditDateTime asOfDateTime)
        where TEvent : IEventBase
    {
        var latest = new Dictionary<InstrumentID, TEvent>();
        foreach (var @event in events)
        {
            if (@event.EventDateTime.Value > valuationDateTime.Value || @event.AuditDateTime.Value > asOfDateTime.Value)
                continue;

            var instrumentID = GetInstrumentID(@event);
            if (!latest.TryGetValue(instrumentID, out var current) || CompareEventOrder(@event, current) > 0)
                latest[instrumentID] = @event;
        }

        return latest;
    }

    private static InstrumentID GetInstrumentID(IEventBase @event) =>
        @event switch
        {
            InstrumentPriceSetEvent priceSetEvent => priceSetEvent.InstrumentID,
            InstrumentIncomeSetEvent incomeSetEvent => incomeSetEvent.InstrumentID,
            _ => throw new InvalidOperationException($"Unsupported instrument value event type '{@event.GetType().Name}'.")
        };

    private static AuditDateTime GetLatestAuditDateTime(EventDateTime valuationDateTime, List<IInstrumentEvent> instrumentEvents, List<IInstrumentPriceEvent> priceEvents, List<IInstrumentIncomeEvent> incomeEvents)
    {
        var latest = instrumentEvents.Cast<IEventBase>()
            .Concat(priceEvents)
            .Concat(incomeEvents)
            .Where(@event => @event.EventDateTime.Value <= valuationDateTime.Value)
            .Select(@event => (DateTime?)@event.AuditDateTime.Value)
            .Max();

        return latest.HasValue ? new AuditDateTime(latest.Value) : AuditDateTimeBuilder.Create();
    }

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

    private sealed record EmptyMarkerEvent(EventID EventID, DateTime AuditDate, EventDateTime EventDateTime, AuditDateTime AsOfDateTime) : IEventBase
    {
        public string Type => nameof(EmptyMarkerEvent);
        public UserID UserID => Constants.Initialisation.UserID;
        public AuditDateTime AuditDateTime => new(AuditDate);
        public string Reason => string.Empty;
    }

    private sealed record InstrumentValuePair(IInstrumentPrice? Price, EventDateTime? PriceValuationDateTime, IInstrumentIncome? Income)
    {
        public static readonly InstrumentValuePair Empty = new(null, null, null);
    }
}
