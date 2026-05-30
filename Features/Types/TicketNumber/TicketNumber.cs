using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(TicketNumberJsonConverter))]
public sealed record TicketNumber : IType
{
    public int Value { get; init; }

    public TicketNumber(int value)
    {
        if (value <= 0)
            throw new ArgumentException("TicketNumber must be greater than zero.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private TicketNumber() { }

    internal static TicketNumber FromJson(int value) => new(value);

    public override string ToString() => Value.ToString();

    public string ToData() => Value.ToString();

    public string ToDetail() => $"{nameof(TicketNumber)}: {Value}";

    public static implicit operator TicketNumber(int value) => new(value);
}

internal sealed class TicketNumberJsonConverter : JsonConverter<TicketNumber>
{
    public override TicketNumber? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : TicketNumber.FromJson(reader.GetInt32());

    public override void Write(Utf8JsonWriter writer, TicketNumber value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
