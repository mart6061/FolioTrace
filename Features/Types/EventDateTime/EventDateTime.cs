using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(EventDateTimeJsonConverter))]
public sealed record EventDateTime : IType
{
    public DateTime Value { get; init; }

    // Regular constructor enforces rules
    public EventDateTime(DateTime value)
    {
        if (value == default)
            throw new ArgumentException("Value must be a non-default DateTime.", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private EventDateTime() { }

    // Factory used by converter to create an instance without validation
    internal static EventDateTime FromJson(string? value) => new EventDateTime { Value = DateTime.Parse(value!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) };

    public static implicit operator DateTime(EventDateTime d) => d?.Value ?? default;

    public static implicit operator EventDateTime(DateTime dt) => new EventDateTime(dt);

    public override string ToString() => Value.ToString("o");
}

internal sealed class EventDateTimeJsonConverter : JsonConverter<EventDateTime>
{
    public override EventDateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for EventDateTime value.");

        var s = reader.GetString();
        return EventDateTime.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, EventDateTime value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == default)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString("o", CultureInfo.InvariantCulture));
    }
}
