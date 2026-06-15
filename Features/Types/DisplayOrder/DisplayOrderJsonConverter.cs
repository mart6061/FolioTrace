using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class DisplayOrderJsonConverter : JsonConverter<DisplayOrder>
{
    public override DisplayOrder? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : DisplayOrder.FromJson(reader.GetInt32());

    public override void Write(Utf8JsonWriter writer, DisplayOrder value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
