using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentValue : IModel
{
    public required InstrumentID InstrumentID { get; init; }
    public required string Name { get; init; }
    public required string FormalName { get; init; }
    public required Exchange Exchange { get; init; }
    public required CFI CFI { get; init; }
    public InstrumentLogo? Logo { get; init; }
    public required bool Active { get; init; }
    public required Alpha2 IncomeCountry { get; init; }
    public required Alpha2 PriceCountry { get; init; }
    public required List<InstrumentIdentifier> Identifiers { get; init; }
    public IInstrumentTerms? Terms { get; init; }
    public IInstrumentPrice? Price { get; init; }
    public IInstrumentIncome? Income { get; init; }
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentValue(Instrument instrument, IInstrumentPrice? price, IInstrumentIncome? income, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        if (instrument is null)
            throw new ArgumentNullException(nameof(instrument));

        InstrumentID = instrument.InstrumentID;
        Name = instrument.Name;
        FormalName = instrument.FormalName;
        Exchange = instrument.Exchange;
        CFI = instrument.CFI;
        Logo = instrument.Logo;
        Active = instrument.Active;
        IncomeCountry = instrument.IncomeCountry;
        PriceCountry = instrument.PriceCountry;
        Identifiers = instrument.Identifiers;
        Terms = instrument.Terms;
        Price = price;
        Income = income;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    public string ToData() => $"{InstrumentID.ToData()}|{Name}|{Price?.ToData()}|{Income?.ToData()}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(InstrumentValue)}: (InstrumentID: {InstrumentID.ToDetail()}, Name: {Name}, Price: {Price?.ToDetail()}, Income: {Income?.ToDetail()})";
}

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
                priceEvent is InstrumentPriceSetEvent priceSetEvent ? priceSetEvent.Price : null,
                incomeEvent is InstrumentIncomeSetEvent incomeSetEvent ? incomeSetEvent.Income : null,
                lastValueEvent?.EventDateTime ?? instrument.ValuationDateTime,
                lastValueEvent?.AuditDateTime ?? instrument.AsOfDateTime,
                lastValueEvent?.EventID ?? instrument.LastEventID,
                new LastAuditDateTime(lastAudit)));
        }
    }

    public string ToData() => $"{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(InstrumentValues)}: (ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastEventID: {LastEventID.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()}, Items: {Items.Count})";

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
        public string ToData() => string.Empty;
        public string ToDetail() => string.Empty;
    }
}
