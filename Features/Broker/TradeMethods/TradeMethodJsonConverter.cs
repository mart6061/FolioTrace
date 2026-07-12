using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

public sealed class TradeMethodJsonConverter : JsonConverter<ITradeMethod>
{
    public override ITradeMethod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var discriminator = ReadString(root, "$type") ?? ReadString(root, nameof(ITradeMethod.Type));

        return discriminator switch
        {
            nameof(FIXTradeMethod) or nameof(TradeMethodType.FIX) => Deserialize<FIXTradeMethod>(root, options),
            nameof(PhoneTradeMethod) or nameof(TradeMethodType.Phone) => Deserialize<PhoneTradeMethod>(root, options),
            nameof(FaxTradeMethod) or nameof(TradeMethodType.Fax) => Deserialize<FaxTradeMethod>(root, options),
            nameof(TradeFileTradeMethod) or nameof(TradeMethodType.TradeFile) => Deserialize<TradeFileTradeMethod>(root, options),
            nameof(ManualTradeMethod) or nameof(TradeMethodType.Manual) => Deserialize<ManualTradeMethod>(root, options),
            _ => throw new JsonException("Trade method type discriminator is missing or unsupported.")
        };
    }

    public override void Write(Utf8JsonWriter writer, ITradeMethod value, JsonSerializerOptions options)
    {
        var element = JsonSerializer.SerializeToElement(value, value.GetType(), options);

        writer.WriteStartObject();
        writer.WriteString("$type", value.GetType().Name);
        foreach (var property in element.EnumerateObject())
            property.WriteTo(writer);
        writer.WriteEndObject();
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();
        }

        return null;
    }

    private static T Deserialize<T>(JsonElement root, JsonSerializerOptions options) where T : ITradeMethod =>
        root.Deserialize<T>(options) ?? throw new JsonException($"Trade method payload could not be deserialized as {typeof(T).Name}.");
}