using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(LastAuditDateTimeJsonConverter))]
public sealed record LastAuditDateTime : IType
{
    public DateTime Value { get; init; }

    // Regular constructor enforces rules
    public LastAuditDateTime(DateTime value)
    {
        if (value == default)
            throw new ArgumentException("Value must be a non-default DateTime.", nameof(value));

        Value = value;
    }

    // JsonConstructor: used by System.Text.Json to populate the object without enforcing validation.
    [JsonConstructor]
    private LastAuditDateTime() { }

    // Factory used by converter to create an instance without validation
    internal static LastAuditDateTime FromJson(string? value) => new LastAuditDateTime { Value = DateTime.Parse(value!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) };

    public static implicit operator DateTime(LastAuditDateTime d) => d?.Value ?? default;

    public static implicit operator LastAuditDateTime(DateTime dt) => new LastAuditDateTime(dt);

    public static implicit operator LastAuditDateTime(AuditDateTime auditDateTime) => new LastAuditDateTime(auditDateTime?.Value ?? default);

    public override string ToString() => Value.ToString("o");
}
