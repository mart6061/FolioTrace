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
}
