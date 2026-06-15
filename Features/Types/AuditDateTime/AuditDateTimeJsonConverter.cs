using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class AuditDateTimeJsonConverter : JsonConverter<AuditDateTime>
{
    public override AuditDateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for AuditDateTime value.");

        var s = reader.GetString();
        return AuditDateTime.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, AuditDateTime value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == default)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString("o", CultureInfo.InvariantCulture));
    }
}
