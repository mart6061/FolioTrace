using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;


namespace FolioTrace.Aggregates;


internal sealed class InstrumentPriceJsonConverter : JsonConverter<IInstrumentPrice>
{
    public override IInstrumentPrice? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var type = ReadDiscriminator(root);

        if (type is null && HasProperty(root, nameof(InstrumentPriceEquity.Bid)))
            type = nameof(InstrumentPriceEquity);

        if (type is null && HasProperty(root, nameof(InstrumentPriceFixedIncome.CleanPrice)))
            type = nameof(InstrumentPriceFixedIncome);

        if (type is null && HasProperty(root, nameof(InstrumentPriceCash.Price)))
            type = nameof(InstrumentPriceCash);

        return type switch
        {
            nameof(InstrumentPriceEquity) => root.Deserialize<InstrumentPriceEquity>(options),
            nameof(InstrumentPriceFixedIncome) => root.Deserialize<InstrumentPriceFixedIncome>(options),
            nameof(InstrumentPriceCash) => root.Deserialize<InstrumentPriceCash>(options),
            _ => throw new JsonException($"Unsupported instrument price type '{type ?? "<missing>"}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, IInstrumentPrice value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case InstrumentPriceEquity equity:
                WriteWithType(writer, equity, nameof(InstrumentPriceEquity), options);
                break;
            case InstrumentPriceFixedIncome fixedIncome:
                WriteWithType(writer, fixedIncome, nameof(InstrumentPriceFixedIncome), options);
                break;
            case InstrumentPriceCash cash:
                WriteWithType(writer, cash, nameof(InstrumentPriceCash), options);
                break;
            default:
                throw new JsonException($"Unsupported instrument price type '{value.GetType().Name}'.");
        }
    }

    private static string? ReadDiscriminator(JsonElement root)
    {
        if (root.TryGetProperty("$type", out var discriminator))
            return discriminator.GetString();

        if (root.TryGetProperty("type", out var lowerDiscriminator))
            return lowerDiscriminator.GetString();

        return null;
    }

    private static bool HasProperty(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out _) || root.TryGetProperty(char.ToLowerInvariant(propertyName[0]) + propertyName[1..], out _);

    private static void WriteWithType<T>(Utf8JsonWriter writer, T value, string type, JsonSerializerOptions options)
    {
        var element = JsonSerializer.SerializeToElement(value, options);
        writer.WriteStartObject();
        writer.WriteString("$type", type);

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("$type"))
                continue;

            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}

internal sealed class InstrumentIncomeJsonConverter : JsonConverter<IInstrumentIncome>
{
    public override IInstrumentIncome? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var type = ReadDiscriminator(root);

        if (type is null && TryGetProperty(root, nameof(InstrumentIncomeCash.Income), out var cashIncome) && cashIncome.ValueKind == JsonValueKind.Number)
            type = nameof(InstrumentIncomeCash);

        if (type is null && (HasProperty(root, nameof(InstrumentIncomeEquity.DividendAmount)) || HasProperty(root, "Income")))
            type = nameof(InstrumentIncomeEquity);

        if (type is null && HasProperty(root, nameof(InstrumentIncomeFixedIncome.AccruedInterest)))
            type = nameof(InstrumentIncomeFixedIncome);

        return type switch
        {
            nameof(InstrumentIncomeEquity) => ReadInstrumentIncomeEquity(root, options),
            nameof(InstrumentIncomeFixedIncome) => root.Deserialize<InstrumentIncomeFixedIncome>(options),
            nameof(InstrumentIncomeCash) => root.Deserialize<InstrumentIncomeCash>(options),
            _ => throw new JsonException($"Unsupported instrument income type '{type ?? "<missing>"}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, IInstrumentIncome value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case InstrumentIncomeEquity equity:
                WriteWithType(writer, equity, nameof(InstrumentIncomeEquity), options);
                break;
            case InstrumentIncomeFixedIncome fixedIncome:
                WriteWithType(writer, fixedIncome, nameof(InstrumentIncomeFixedIncome), options);
                break;
            case InstrumentIncomeCash cash:
                WriteWithType(writer, cash, nameof(InstrumentIncomeCash), options);
                break;
            default:
                throw new JsonException($"Unsupported instrument income type '{value.GetType().Name}'.");
        }
    }

    private static string? ReadDiscriminator(JsonElement root)
    {
        if (root.TryGetProperty("$type", out var discriminator))
            return discriminator.GetString();

        if (root.TryGetProperty("type", out var lowerDiscriminator))
            return lowerDiscriminator.GetString();

        return null;
    }

    private static bool HasProperty(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out _) || root.TryGetProperty(char.ToLowerInvariant(propertyName[0]) + propertyName[1..], out _);

    private static InstrumentIncomeEquity? ReadInstrumentIncomeEquity(JsonElement root, JsonSerializerOptions options)
    {
        if (TryGetProperty(root, nameof(InstrumentIncomeEquity.DividendAmount), out _))
            return root.Deserialize<InstrumentIncomeEquity>(options);

        var dividendAmount = TryGetProperty(root, "Income", out var legacyIncome)
            ? legacyIncome.Deserialize<InstrumentPrice>(options)
            : new InstrumentPrice(null);

        return new InstrumentIncomeEquity(
            dividendAmount ?? new InstrumentPrice(null),
            TryGetProperty(root, nameof(InstrumentIncomeEquity.DividendType), out var dividendType) ? dividendType.GetString() ?? string.Empty : string.Empty,
            TryGetProperty(root, nameof(InstrumentIncomeEquity.ExDividend), out var exDividend) ? exDividend.Deserialize<InstrumentDate>(options) ?? new InstrumentDate(null) : new InstrumentDate(null),
            TryGetProperty(root, nameof(InstrumentIncomeEquity.Declaration), out var declaration) ? declaration.Deserialize<InstrumentDate>(options) ?? new InstrumentDate(null) : new InstrumentDate(null),
            TryGetProperty(root, nameof(InstrumentIncomeEquity.Record), out var record) ? record.Deserialize<InstrumentDate>(options) ?? new InstrumentDate(null) : new InstrumentDate(null),
            TryGetProperty(root, nameof(InstrumentIncomeEquity.Payable), out var payable) ? payable.Deserialize<InstrumentDate>(options) ?? new InstrumentDate(null) : new InstrumentDate(null));
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement property) =>
        root.TryGetProperty(propertyName, out property) || root.TryGetProperty(char.ToLowerInvariant(propertyName[0]) + propertyName[1..], out property);

    private static void WriteWithType<T>(Utf8JsonWriter writer, T value, string type, JsonSerializerOptions options)
    {
        var element = JsonSerializer.SerializeToElement(value, options);
        writer.WriteStartObject();
        writer.WriteString("$type", type);

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("$type"))
                continue;

            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
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
                priceEvent?.EventDateTime,
                incomeEvent is InstrumentIncomeSetEvent incomeSetEvent ? incomeSetEvent.Income : null,
                lastValueEvent?.EventDateTime ?? instrument.ValuationDateTime,
                lastValueEvent?.AuditDateTime ?? instrument.AsOfDateTime,
                lastValueEvent?.EventID ?? instrument.LastEventID,
                new LastAuditDateTime(lastAudit)));
        }
    }

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
}
