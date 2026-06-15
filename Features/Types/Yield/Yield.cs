using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(YieldJsonConverter))]
public sealed record Yield : IType
{
    public decimal Value { get; init; }

    public Yield(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Yield must be zero or greater.", nameof(value));

        if (decimal.Round(value, 8) != value)
            throw new ArgumentException("Yield can have at most 8 decimal places.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private Yield()
    {
    }

    internal static Yield FromJson(decimal value) => new(value);

    public override string ToString() => Value.ToString("0.########");
}
