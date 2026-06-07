using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(AssetAllocationIDJsonConverter))]
public sealed record AssetAllocationID : IType
{
    public Guid Value { get; init; }

    public AssetAllocationID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private AssetAllocationID() { }

    internal static AssetAllocationID FromJson(string? value) => new() { Value = Guid.Parse(value!) };

    public static implicit operator Guid(AssetAllocationID id) => id?.Value ?? Guid.Empty;

    public static implicit operator AssetAllocationID(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

internal sealed class AssetAllocationIDJsonConverter : JsonConverter<AssetAllocationID>
{
    public override AssetAllocationID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for AssetAllocationID value.");

        return AssetAllocationID.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, AssetAllocationID value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == Guid.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
