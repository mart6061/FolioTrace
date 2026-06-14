using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(ReportIDJsonConverter))]
public sealed record ReportID : IType
{
    public Guid Value { get; init; }

    public ReportID(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value must be a non-empty GUID.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private ReportID() { }

    internal static ReportID FromJson(string? value) => new() { Value = Guid.Parse(value!) };

    public static implicit operator Guid(ReportID id) => id?.Value ?? Guid.Empty;

    public static implicit operator ReportID(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

internal sealed class ReportIDJsonConverter : JsonConverter<ReportID>
{
    public override ReportID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for ReportID value.");

        return ReportID.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, ReportID value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == Guid.Empty)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
