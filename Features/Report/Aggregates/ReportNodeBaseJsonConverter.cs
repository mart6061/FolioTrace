using System.Text.Json;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal sealed class ReportNodeBaseJsonConverter : JsonConverter<ReportNodeBase>
{
    public override ReportNodeBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var type = ReadDiscriminator(root) ?? InferDiscriminator(root);
        var reportNodeID = ReadRequired<ReportNodeID>(root, nameof(ReportNodeBase.ReportNodeID), options);
        var displayOrder = ReadRequired<int>(root, nameof(ReportNodeBase.DisplayOrder), options);
        var name = ReadRequired<string>(root, nameof(ReportNodeBase.Name), options);
        var title = ReadRequired<string>(root, nameof(ReportNodeBase.Title), options);
        var pageOrientation = ReadOptional(root, nameof(ReportNodeBase.PageOrientation), options, ReportNodePageOrientation.Portrait);

        return type switch
        {
            nameof(ReportNodeCoverPage) => new ReportNodeCoverPage(reportNodeID, displayOrder, name, title) { PageOrientation = pageOrientation },
            nameof(ReportNodeIndex) => new ReportNodeIndex(reportNodeID, displayOrder, name, title) { PageOrientation = pageOrientation },
            nameof(ReportNodeChart) => new ReportNodeChart(
                reportNodeID,
                displayOrder,
                name,
                title,
                ReadRequired<AssetAllocationID>(root, nameof(ReportNodeChart.AssetAllocationID), options),
                ReadOptional(root, nameof(ReportNodeChart.ChartType), options, ReportChartType.Pie))
            {
                PageOrientation = pageOrientation,
                PieLevel = ReadOptional(root, nameof(ReportNodeChart.PieLevel), options, 1)
            },
            nameof(ReportNodeValuation) => new ReportNodeValuation(
                reportNodeID,
                displayOrder,
                name,
                title,
                ReadRequired<AssetAllocationID>(root, nameof(ReportNodeValuation.AssetAllocationID), options),
                ReadOptional<List<ReportValuationColumn>?>(root, nameof(ReportNodeValuation.Columns), options, null),
                ReadOptional(root, nameof(ReportNodeValuation.ColourBullet), options, true),
                ReadOptional(root, nameof(ReportNodeValuation.ColourText), options, false),
                ReadOptional(root, nameof(ReportNodeValuation.DisplayHoldings), options, true))
            {
                PageOrientation = pageOrientation
            },
            nameof(ReportNodeTransactions) => new ReportNodeTransactions(
                reportNodeID,
                displayOrder,
                name,
                title,
                ReadRequired<AssetAllocationID>(root, nameof(ReportNodeTransactions.AssetAllocationID), options))
            {
                PageOrientation = pageOrientation
            },
            nameof(ReportNodeProfitLoss) => new ReportNodeProfitLoss(
                reportNodeID,
                displayOrder,
                name,
                title,
                ReadRequired<AssetAllocationID>(root, nameof(ReportNodeProfitLoss.AssetAllocationID), options),
                ReadOptional(root, nameof(ReportNodeProfitLoss.ProfitLossMethod), options, ReportProfitLossMethod.Default))
            {
                PageOrientation = pageOrientation
            },
            nameof(ReportNodeCash) => new ReportNodeCash(
                reportNodeID,
                displayOrder,
                name,
                title,
                ReadRequired<AssetAllocationID>(root, nameof(ReportNodeCash.AssetAllocationID), options))
            {
                PageOrientation = pageOrientation
            },
            _ => throw new JsonException($"Unsupported report node type '{type ?? "<missing>"}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, ReportNodeBase value, JsonSerializerOptions options)
    {
        var type = value.GetType().Name;
        var element = JsonSerializer.SerializeToElement(value, value.GetType(), options);

        writer.WriteStartObject();
        writer.WriteString("$type", type);

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("$type") || property.NameEquals("type") || property.NameEquals("Type"))
                continue;

            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }

    private static string? ReadDiscriminator(JsonElement root)
    {
        if (root.TryGetProperty("$type", out var discriminator))
            return discriminator.GetString();

        if (root.TryGetProperty("type", out var lowerDiscriminator))
            return lowerDiscriminator.GetString();

        if (root.TryGetProperty("Type", out var upperDiscriminator))
            return upperDiscriminator.GetString();

        return null;
    }

    private static string? InferDiscriminator(JsonElement root)
    {
        if (HasProperty(root, nameof(ReportNodeChart.ChartType)))
            return nameof(ReportNodeChart);

        if (HasProperty(root, nameof(ReportNodeValuation.Columns))
            || HasProperty(root, nameof(ReportNodeValuation.ColourBullet))
            || HasProperty(root, nameof(ReportNodeValuation.ColourText))
            || HasProperty(root, nameof(ReportNodeValuation.DisplayHoldings)))
            return nameof(ReportNodeValuation);

        if (HasProperty(root, nameof(ReportNodeProfitLoss.ProfitLossMethod)))
            return nameof(ReportNodeProfitLoss);

        if (TryGetProperty(root, nameof(ReportNodeBase.Name), out var nameProperty))
        {
            var name = nameProperty.GetString();
            if (string.Equals(name, "Cover Page", StringComparison.OrdinalIgnoreCase))
                return nameof(ReportNodeCoverPage);

            if (string.Equals(name, "Index", StringComparison.OrdinalIgnoreCase))
                return nameof(ReportNodeIndex);

            if (string.Equals(name, "Transactions", StringComparison.OrdinalIgnoreCase))
                return nameof(ReportNodeTransactions);

            if (string.Equals(name, "Profit Loss", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "ProfitLoss", StringComparison.OrdinalIgnoreCase))
                return nameof(ReportNodeProfitLoss);

            if (string.Equals(name, "Cash", StringComparison.OrdinalIgnoreCase))
                return nameof(ReportNodeCash);
        }

        return null;
    }

    private static T ReadRequired<T>(JsonElement root, string propertyName, JsonSerializerOptions options)
    {
        if (!TryGetProperty(root, propertyName, out var property))
            throw new JsonException($"Report node requires '{propertyName}'.");

        return property.Deserialize<T>(options) ?? throw new JsonException($"Report node property '{propertyName}' is invalid.");
    }

    private static T ReadOptional<T>(JsonElement root, string propertyName, JsonSerializerOptions options, T fallback)
    {
        if (!TryGetProperty(root, propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return fallback;

        return property.Deserialize<T>(options) ?? fallback;
    }

    private static bool HasProperty(JsonElement root, string propertyName) =>
        TryGetProperty(root, propertyName, out _);

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement property)
    {
        if (root.TryGetProperty(propertyName, out property))
            return true;

        return root.TryGetProperty(char.ToLowerInvariant(propertyName[0]) + propertyName[1..], out property);
    }
}
