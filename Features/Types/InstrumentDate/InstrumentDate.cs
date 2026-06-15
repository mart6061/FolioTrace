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
