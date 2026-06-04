using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(ActiveJsonConverter))]
public sealed record Active : IType
{
    public bool Value { get; init; }

    public Active(bool value)
    {
        Value = value;
    }

    [JsonConstructor]
    private Active() { }

    internal static Active FromJson(bool value) => new(value);

    public override string ToString() => Value.ToString();

    public string ToData() => Value.ToString();

    public string ToDetail() => $"{nameof(Active)}: {Value}";

    public static implicit operator Active(bool value) => new(value);

    public static implicit operator bool(Active active) => active?.Value ?? false;
}

internal sealed class ActiveJsonConverter : JsonConverter<Active>
{
    public override Active? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Active.FromJson(reader.GetBoolean());

    public override void Write(Utf8JsonWriter writer, Active value, JsonSerializerOptions options) =>
        writer.WriteBooleanValue(value.Value);
}
