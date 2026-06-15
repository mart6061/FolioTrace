using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class SettlementDateTimeJsonConverter : JsonConverter<SettlementDateTime>
{
    public override SettlementDateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for SettlementDateTime value.");

        var value = reader.GetString();
        return SettlementDateTime.FromJson(value);
    }

    public override void Write(Utf8JsonWriter writer, SettlementDateTime value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == default)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString("o", CultureInfo.InvariantCulture));
    }
}
