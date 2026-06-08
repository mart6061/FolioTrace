using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(NodeIDJsonConverter))]
public sealed record NodeID : IType
{
    public Guid Value { get; init; }

    public NodeID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private NodeID() { }

    internal static NodeID FromJson(string? value) => new() { Value = Guid.Parse(value!) };

    public static implicit operator Guid(NodeID id) => id?.Value ?? Guid.Empty;

    public static implicit operator NodeID(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

internal sealed class NodeIDJsonConverter : JsonConverter<NodeID>
{
    public override NodeID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for NodeID value.");

        return NodeID.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, NodeID value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == Guid.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
