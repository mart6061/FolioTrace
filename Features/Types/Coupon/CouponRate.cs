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

    public string ToData() => Value.ToString("0.########");

    public string ToDetail() => $"{nameof(CouponRate)}: {this}";
}

public enum CouponFrequency
{
    None = 0,
    Annual = 1,
    SemiAnnual = 2,
    Quarterly = 4,
    Monthly = 12
}

internal sealed class CouponRateJsonConverter : JsonConverter<CouponRate>
{
    public override CouponRate? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : CouponRate.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, CouponRate value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
