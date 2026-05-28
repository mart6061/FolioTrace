using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(SettlementDateTimeJsonConverter))]
public sealed record SettlementDateTime : IType
{
    public DateTime Value { get; init; }

    public SettlementDateTime(DateTime value)
    {
        if (value == default)
            throw new ArgumentException("Value must be a non-default DateTime.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private SettlementDateTime() { }

    internal static SettlementDateTime FromJson(string? value) => new() { Value = DateTime.Parse(value!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) };

    public static implicit operator DateTime(SettlementDateTime d) => d?.Value ?? default;

    public static implicit operator SettlementDateTime(DateTime dt) => new(dt);

    public override string ToString() => Value.ToString("o");

    public string ToData() => Value.ToString("o");

    public string ToDetail() => $"{nameof(SettlementDateTime)}: {this}";
}

internal sealed class SettlementDateTimeJsonConverter : JsonConverter<SettlementDateTime>
{
    public override SettlementDateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string token for SettlementDateTime value.");

        var value = reader.GetString();
        return SettlementDateTime.FromJson(value);
    }

    public override void Write(Utf8JsonWriter writer, SettlementDateTime value, JsonSerializerOptions options)
    {
        if (value is null || value.Value == default)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString("o", CultureInfo.InvariantCulture));
    }
}
