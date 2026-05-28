using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(HoldingIDJsonConverter))]
public sealed record HoldingID : IType
{
    public Guid Value { get; init; }

    public HoldingID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private HoldingID() { }

    internal static HoldingID FromJson(string? value) => new() { Value = Guid.Parse(value!) };

    public static implicit operator Guid(HoldingID id) => id?.Value ?? Guid.Empty;

    public static implicit operator HoldingID(Guid g) => new(g);

    public override string ToString() => Value.ToString();

    public string ToData() => Value.ToString();

    public string ToDetail() => $"{nameof(HoldingID)}: {this}";
}

internal sealed class HoldingIDJsonConverter : JsonConverter<HoldingID>
{
    public override HoldingID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for HoldingID value.");

        return HoldingID.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, HoldingID value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == Guid.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
