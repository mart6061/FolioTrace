using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal sealed class CurrencyPairJsonConverter : JsonConverter<CurrencyPair>
{
    public override CurrencyPair? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for CurrencyPair value.");

        return CurrencyPair.FromJson(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, CurrencyPair value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
