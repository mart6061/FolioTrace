using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(CouponRateJsonConverter))]
public sealed record CouponRate : IType
{
    public decimal Value { get; init; }

    public CouponRate(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Coupon rate must be zero or greater.", nameof(value));

        if (decimal.Round(value, 8) != value)
            throw new ArgumentException("Coupon rate can have at most 8 decimal places.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private CouponRate() { }

    internal static CouponRate FromJson(decimal value) => new(value);

    public override string ToString() => Value.ToString("0.########");
}
