using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(EventSetIDJsonConverter))]
public sealed record EventSetID : IType
{
    public Guid Value { get; init; }

    public EventSetID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private EventSetID() { }

    internal static EventSetID FromJson(string? value) => new() { Value = Guid.Parse(value!) };

    public static implicit operator Guid(EventSetID id) => id?.Value ?? Guid.Empty;

    public static implicit operator EventSetID(Guid g) => new(g);

    public override string ToString() => Value.ToString();

    public string ToData() => Value.ToString();

    public string ToDetail() => $"{nameof(EventSetID)}: {this}";
}

internal sealed class EventSetIDJsonConverter : JsonConverter<EventSetID>
{
    public override EventSetID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for EventSetID value.");

        return EventSetID.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, EventSetID value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == Guid.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
