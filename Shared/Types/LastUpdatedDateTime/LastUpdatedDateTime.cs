using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AILibrary.Types;

[JsonConverter(typeof(LastUpdatedDateTimeJsonConverter))]
public sealed record LastUpdatedDateTime : IType
{
    public DateTime Value { get; init; }

    // Regular constructor enforces rules
    public LastUpdatedDateTime(DateTime value)
    {
        if (value == default)
            throw new ArgumentException("Value must be a non-default DateTime.", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private LastUpdatedDateTime() { }

    // Factory used by converter to create an instance without validation
    internal static LastUpdatedDateTime FromJson(string? value) => new LastUpdatedDateTime { Value = DateTime.Parse(value!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) };

    public static implicit operator DateTime(LastUpdatedDateTime d) => d?.Value ?? default;

    public static implicit operator LastUpdatedDateTime(DateTime dt) => new LastUpdatedDateTime(dt);`r`n`r`n    public static implicit operator LastUpdatedDateTime(AuditDateTime auditDateTime) => new LastUpdatedDateTime(auditDateTime?.Value ?? default);

    public override string ToString() => Value.ToString("o");

    public string ToData() => Value.ToString("o");

    public string ToDetail() => $"{nameof(LastUpdatedDateTime)}: {this}";
}

internal sealed class LastUpdatedDateTimeJsonConverter : JsonConverter<LastUpdatedDateTime>
{
    public override LastUpdatedDateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for LastUpdatedDateTime value.");

        var s = reader.GetString();
        return LastUpdatedDateTime.FromJson(s);
    }

    public override void Write(Utf8JsonWriter writer, LastUpdatedDateTime value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == default)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString("o", CultureInfo.InvariantCulture));
    }
}

