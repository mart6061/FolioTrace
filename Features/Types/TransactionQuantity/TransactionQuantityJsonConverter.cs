using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class TransactionQuantityJsonConverter : JsonConverter<TransactionQuantity>
{
    public override TransactionQuantity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : TransactionQuantity.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, TransactionQuantity value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
