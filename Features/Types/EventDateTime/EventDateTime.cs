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
