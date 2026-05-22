using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(InstrumentPriceJsonConverter))]
public interface IInstrumentPrice : IType
{
    string PriceType { get; }
}

[JsonConverter(typeof(InstrumentIncomeJsonConverter))]
public interface IInstrumentIncome : IType
{
    string IncomeType { get; }
}

public sealed record InstrumentPriceEquity : IInstrumentPrice
{
    public required Money Bid { get; init; }

    public required Money Mid { get; init; }

    public required Money Ask { get; init; }

    public required Money Nav { get; init; }

    public string PriceType => nameof(InstrumentPriceEquity);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentPriceEquity(Money bid, Money mid, Money ask, Money nav)
    {
        Bid = bid ?? throw new ArgumentNullException(nameof(bid));
        Mid = mid ?? throw new ArgumentNullException(nameof(mid));
        Ask = ask ?? throw new ArgumentNullException(nameof(ask));
        Nav = nav ?? throw new ArgumentNullException(nameof(nav));

        if (Bid.Currency != Mid.Currency || Mid.Currency != Ask.Currency || Ask.Currency != Nav.Currency)
            throw new ArgumentException("All equity price values must use the same currency.");

        if (Bid.Amount > Mid.Amount)
            throw new ArgumentException("Bid must be less than or equal to mid.", nameof(bid));

        if (Mid.Amount > Ask.Amount)
            throw new ArgumentException("Mid must be less than or equal to ask.", nameof(mid));
    }

    public string ToData() => $"{Bid.ToData()}|{Mid.ToData()}|{Ask.ToData()}|{Nav.ToData()}";

    public string ToDetail() => $"{nameof(InstrumentPriceEquity)}: (Bid: {Bid.ToDetail()}, Mid: {Mid.ToDetail()}, Ask: {Ask.ToDetail()}, Nav: {Nav.ToDetail()})";
}

public sealed record InstrumentIncomeEquity : IInstrumentIncome
{
    public required Money Income { get; init; }

    public string IncomeType => nameof(InstrumentIncomeEquity);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentIncomeEquity(Money income)
    {
        Income = income ?? throw new ArgumentNullException(nameof(income));
    }

    public string ToData() => Income.ToData();

    public string ToDetail() => $"{nameof(InstrumentIncomeEquity)}: (Income: {Income.ToDetail()})";
}

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

        return type switch
        {
            nameof(InstrumentPriceEquity) => root.Deserialize<InstrumentPriceEquity>(options),
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

        if (type is null && HasProperty(root, nameof(InstrumentIncomeEquity.Income)))
            type = nameof(InstrumentIncomeEquity);

        return type switch
        {
            nameof(InstrumentIncomeEquity) => root.Deserialize<InstrumentIncomeEquity>(options),
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
