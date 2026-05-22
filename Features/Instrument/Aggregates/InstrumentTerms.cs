using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(InstrumentTermsJsonConverter))]
public interface IInstrumentTerms : IType
{
    string TermsType { get; }
}

public sealed record InstrumentTermsEquity : IInstrumentTerms
{
    public string TermsType => nameof(InstrumentTermsEquity);

    public string ToData() => TermsType;

    public string ToDetail() => $"{nameof(InstrumentTermsEquity)}";
}

public sealed record InstrumentTermsBond : IInstrumentTerms
{
    public required Money ParAmount { get; init; }

    public required CouponRate CouponRate { get; init; }

    public required CouponFrequency CouponFrequency { get; init; }

    public required DateTime MaturityDate { get; init; }

    public DateTime? IssueDate { get; init; }

    public string? DayCount { get; init; }

    public string TermsType => nameof(InstrumentTermsBond);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentTermsBond(Money parAmount, CouponRate couponRate, CouponFrequency couponFrequency, DateTime maturityDate, DateTime? issueDate = null, string? dayCount = null)
    {
        ParAmount = parAmount ?? throw new ArgumentNullException(nameof(parAmount));
        CouponRate = couponRate ?? throw new ArgumentNullException(nameof(couponRate));
        CouponFrequency = couponFrequency;
        MaturityDate = maturityDate;
        IssueDate = issueDate;
        DayCount = dayCount;
    }

    public string ToData() => $"{TermsType}|{ParAmount.ToData()}|{CouponRate.ToData()}|{CouponFrequency}|{MaturityDate:O}|{IssueDate:O}|{DayCount}";

    public string ToDetail() => $"{nameof(InstrumentTermsBond)}: (ParAmount: {ParAmount.ToDetail()}, CouponRate: {CouponRate.ToDetail()}, CouponFrequency: {CouponFrequency}, MaturityDate: {MaturityDate:O}, IssueDate: {IssueDate:O}, DayCount: {DayCount})";
}

internal sealed class InstrumentTermsJsonConverter : JsonConverter<IInstrumentTerms>
{
    public override IInstrumentTerms? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var type = ReadDiscriminator(root);

        if (type is null && HasProperty(root, nameof(InstrumentTermsBond.ParAmount)))
            type = nameof(InstrumentTermsBond);

        if (type is null)
            type = nameof(InstrumentTermsEquity);

        return type switch
        {
            nameof(InstrumentTermsEquity) => root.Deserialize<InstrumentTermsEquity>(options) ?? new InstrumentTermsEquity(),
            nameof(InstrumentTermsBond) => root.Deserialize<InstrumentTermsBond>(options),
            _ => throw new JsonException($"Unsupported instrument terms type '{type}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, IInstrumentTerms value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case InstrumentTermsEquity equity:
                WriteWithType(writer, equity, nameof(InstrumentTermsEquity), options);
                break;
            case InstrumentTermsBond bond:
                WriteWithType(writer, bond, nameof(InstrumentTermsBond), options);
                break;
            default:
                throw new JsonException($"Unsupported instrument terms type '{value.GetType().Name}'.");
        }
    }

    private static string? ReadDiscriminator(JsonElement root)
    {
        if (root.TryGetProperty("$type", out var discriminator))
            return discriminator.GetString();

        if (root.TryGetProperty("type", out var lowerDiscriminator))
            return lowerDiscriminator.GetString();

        return null;
    }

    private static bool HasProperty(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out _) || root.TryGetProperty(char.ToLowerInvariant(propertyName[0]) + propertyName[1..], out _);

    private static void WriteWithType<T>(Utf8JsonWriter writer, T value, string type, JsonSerializerOptions options)
    {
        var element = JsonSerializer.SerializeToElement(value, options);
        writer.WriteStartObject();
        writer.WriteString("$type", type);

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("$type"))
                continue;

            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
