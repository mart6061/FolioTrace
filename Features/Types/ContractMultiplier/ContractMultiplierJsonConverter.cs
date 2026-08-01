using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

internal sealed class ContractMultiplierJsonConverter : JsonConverter<ContractMultiplier>
{
    public override ContractMultiplier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : ContractMultiplier.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, ContractMultiplier value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
