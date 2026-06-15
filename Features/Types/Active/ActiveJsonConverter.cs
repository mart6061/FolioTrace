using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class ActiveJsonConverter : JsonConverter<Active>
{
    public override Active? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Active.FromJson(reader.GetBoolean());

    public override void Write(Utf8JsonWriter writer, Active value, JsonSerializerOptions options) =>
        writer.WriteBooleanValue(value.Value);
}
