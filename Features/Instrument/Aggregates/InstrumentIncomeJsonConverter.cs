using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;
using FolioTrace.Common;


namespace FolioTrace.Aggregates;

internal sealed class InstrumentIncomeJsonConverter : JsonConverter<IInstrumentIncome>
{
    public override IInstrumentIncome? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var type = ReadDiscriminator(root);

        if (type is null && TryGetProperty(root, nameof(InstrumentIncomeCash.Income), out var cashIncome) && cashIncome.ValueKind == JsonValueKind.Number)
            type = nameof(InstrumentIncomeCash);

        if (type is null && (HasProperty(root, nameof(InstrumentIncomeEquity.DividendAmount)) || HasProperty(root, "Income")))
            type = nameof(InstrumentIncomeEquity);

        if (type is null && HasProperty(root, nameof(InstrumentIncomeFixedIncome.AccruedInterest)))
            type = nameof(InstrumentIncomeFixedIncome);

        return type switch
        {
            nameof(InstrumentIncomeEquity) => ReadInstrumentIncomeEquity(root, options),
            nameof(InstrumentIncomeFixedIncome) => root.Deserialize<InstrumentIncomeFixedIncome>(options),
            nameof(InstrumentIncomeCash) => root.Deserialize<InstrumentIncomeCash>(options),
            _ => throw new JsonException($"Unsupported instrument income type '{type ?? "<missing>"}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, IInstrumentIncome value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case InstrumentIncomeEquity equity:
                WriteWithType(writer, equity, nameof(InstrumentIncomeEquity), options);
                break;
            case InstrumentIncomeFixedIncome fixedIncome:
                WriteWithType(writer, fixedIncome, nameof(InstrumentIncomeFixedIncome), options);
                break;
            case InstrumentIncomeCash cash:
                WriteWithType(writer, cash, nameof(InstrumentIncomeCash), options);
                break;
            default:
                throw new JsonException($"Unsupported instrument income type '{value.GetType().Name}'.");
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

    private static InstrumentIncomeEquity? ReadInstrumentIncomeEquity(JsonElement root, JsonSerializerOptions options)
    {
        if (TryGetProperty(root, nameof(InstrumentIncomeEquity.DividendAmount), out _))
            return root.Deserialize<InstrumentIncomeEquity>(options);

        var dividendAmount = TryGetProperty(root, "Income", out var legacyIncome)
            ? legacyIncome.Deserialize<InstrumentPrice>(options)
            : new InstrumentPrice(null);

        return new InstrumentIncomeEquity(
            dividendAmount ?? new InstrumentPrice(null),
            TryGetProperty(root, nameof(InstrumentIncomeEquity.DividendType), out var dividendType) ? dividendType.GetString() ?? string.Empty : string.Empty,
            TryGetProperty(root, nameof(InstrumentIncomeEquity.ExDividend), out var exDividend) ? exDividend.Deserialize<InstrumentDate>(options) ?? new InstrumentDate(null) : new InstrumentDate(null),
            TryGetProperty(root, nameof(InstrumentIncomeEquity.Declaration), out var declaration) ? declaration.Deserialize<InstrumentDate>(options) ?? new InstrumentDate(null) : new InstrumentDate(null),
            TryGetProperty(root, nameof(InstrumentIncomeEquity.Record), out var record) ? record.Deserialize<InstrumentDate>(options) ?? new InstrumentDate(null) : new InstrumentDate(null),
            TryGetProperty(root, nameof(InstrumentIncomeEquity.Payable), out var payable) ? payable.Deserialize<InstrumentDate>(options) ?? new InstrumentDate(null) : new InstrumentDate(null));
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement property) =>
        root.TryGetProperty(propertyName, out property) || root.TryGetProperty(char.ToLowerInvariant(propertyName[0]) + propertyName[1..], out property);

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
