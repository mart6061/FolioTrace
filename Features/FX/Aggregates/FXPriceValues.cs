using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record FXPriceValue : IType
{
    public decimal Value { get; init; }

    protected FXPriceValue(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("FX price values must be zero or greater.", nameof(value));

        if (decimal.Round(value, 8) != value)
            throw new ArgumentException("FX price values can have at most 8 decimal places.", nameof(value));

        Value = value;
    }

    protected FXPriceValue()
    {
    }

    public override string ToString() => Value.ToString("0.########");

    public string ToData() => Value.ToString("0.########");

    public string ToDetail() => $"{GetType().Name}: {this}";
}

[JsonConverter(typeof(BidJsonConverter))]
public sealed record Bid : FXPriceValue
{
    public Bid(decimal value) : base(value) { }

    [JsonConstructor]
    private Bid() { }

    internal static Bid FromJson(decimal value) => new(value);
}

[JsonConverter(typeof(MidJsonConverter))]
public sealed record Mid : FXPriceValue
{
    public Mid(decimal value) : base(value) { }

    [JsonConstructor]
    private Mid() { }

    internal static Mid FromJson(decimal value) => new(value);
}

[JsonConverter(typeof(AskJsonConverter))]
public sealed record Ask : FXPriceValue
{
    public Ask(decimal value) : base(value) { }

    [JsonConstructor]
    private Ask() { }

    internal static Ask FromJson(decimal value) => new(value);
}

internal sealed class BidJsonConverter : JsonConverter<Bid>
{
    public override Bid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Bid.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, Bid value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}

internal sealed class MidJsonConverter : JsonConverter<Mid>
{
    public override Mid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Mid.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, Mid value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}

internal sealed class AskJsonConverter : JsonConverter<Ask>
{
    public override Ask? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Ask.FromJson(reader.GetDecimal());

    public override void Write(Utf8JsonWriter writer, Ask value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
