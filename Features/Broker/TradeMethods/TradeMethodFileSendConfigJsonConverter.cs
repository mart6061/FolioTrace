using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

public sealed class TradeMethodFileSendConfigJsonConverter : JsonConverter<ITradeMethodFileSendConfig>
{
    public override ITradeMethodFileSendConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var discriminator = ReadString(root, "$type");

        if (discriminator is not null)
        {
            return discriminator switch
            {
                nameof(EmailTradeMethodFileSendConfig) => Deserialize<EmailTradeMethodFileSendConfig>(root, options),
                nameof(FTPTradeMethodFileSendConfig) => Deserialize<FTPTradeMethodFileSendConfig>(root, options),
                _ => throw new JsonException($"Trade method file send configuration type '{discriminator}' is unsupported.")
            };
        }

        // Events written before send configurations became polymorphic contain no
        // discriminator. Their property sets uniquely identify the concrete type.
        if (HasProperty(root, nameof(FTPTradeMethodFileSendConfig.Host)))
            return Deserialize<FTPTradeMethodFileSendConfig>(root, options);

        if (HasProperty(root, nameof(EmailTradeMethodFileSendConfig.To)))
            return Deserialize<EmailTradeMethodFileSendConfig>(root, options);

        throw new JsonException("Trade method file send configuration type discriminator is missing and the legacy payload type could not be inferred.");
    }

    public override void Write(Utf8JsonWriter writer, ITradeMethodFileSendConfig value, JsonSerializerOptions options)
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

    private static bool HasProperty(JsonElement root, string propertyName) =>
        root.EnumerateObject().Any(property => property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

    private static T Deserialize<T>(JsonElement root, JsonSerializerOptions options) where T : ITradeMethodFileSendConfig =>
        root.Deserialize<T>(options) ?? throw new JsonException($"Trade method file send configuration payload could not be deserialized as {typeof(T).Name}.");
}
