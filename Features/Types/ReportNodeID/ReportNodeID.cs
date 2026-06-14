using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(ReportNodeIDJsonConverter))]
public sealed record ReportNodeID : IType
{
    public Guid Value { get; init; }

    public ReportNodeID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private ReportNodeID() { }

    internal static ReportNodeID FromJson(string? value) => new() { Value = Guid.Parse(value!) };

    public static implicit operator Guid(ReportNodeID id) => id?.Value ?? Guid.Empty;

    public static implicit operator ReportNodeID(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

internal sealed class ReportNodeIDJsonConverter : JsonConverter<ReportNodeID>
{
    public override ReportNodeID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for ReportNodeID value.");

        return ReportNodeID.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, ReportNodeID value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == Guid.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
