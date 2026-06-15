using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(AuditDateTimeJsonConverter))]
public sealed record AuditDateTime : IType
{
    public DateTime Value { get; init; }

    // Regular constructor enforces rules
    public AuditDateTime(DateTime value)
    {
        if (value == default)
            throw new ArgumentException("Value must be a non-default DateTime.", nameof(value));

        if (value.ToUniversalTime() > DateTime.UtcNow)
            throw new ArgumentException("Value must not be in the future.", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private AuditDateTime() { }

    // Factory used by converter to create an instance with validation
    internal static AuditDateTime FromJson(string? value) => new AuditDateTime(DateTime.Parse(value!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    public static implicit operator DateTime(AuditDateTime d) => d?.Value ?? default;

    public static implicit operator AuditDateTime(DateTime dt) => new AuditDateTime(dt);

    public override string ToString() => Value.ToString("o");
}
