using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(InstrumentDateJsonConverter))]
public sealed record InstrumentDate : IType
{
    public DateOnly? Value { get; init; }

    public InstrumentDate(DateOnly? value)
    {
        if (value == default(DateOnly))
            throw new ArgumentException("Value must be a non-default DateOnly.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private InstrumentDate() { }

    internal static InstrumentDate FromJson(string? value) =>
        new InstrumentDate { Value = value is null ? null : DateOnly.Parse(value, CultureInfo.InvariantCulture) };

    public static implicit operator DateOnly?(InstrumentDate d) => d?.Value;

    public static implicit operator InstrumentDate(DateOnly? date) => new InstrumentDate(date);

    public override string ToString() => Value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
}

internal sealed class InstrumentDateJsonConverter : JsonConverter<InstrumentDate>
{
    public override InstrumentDate? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for InstrumentDate value.");

        return InstrumentDate.FromJson(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, InstrumentDate value, JsonSerializerOptions options)
    {
        if (value is null || !value.Value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
