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
