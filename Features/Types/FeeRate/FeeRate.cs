using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(FeeRateJsonConverter))]
public sealed record FeeRate : IType
{
    public decimal Value { get; init; }

    public FeeRate(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("FeeRate must be zero or greater.", nameof(value));

        if (decimal.Round(value, 8) != value)
            throw new ArgumentException("FeeRate can have at most 8 decimal places.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private FeeRate() { }

    internal static FeeRate FromJson(decimal value) => new(value);

    public override string ToString() => Value.ToString("0.########");
}
