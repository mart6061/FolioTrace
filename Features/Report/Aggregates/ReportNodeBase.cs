using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ReportNodeCoverPage), nameof(ReportNodeCoverPage))]
[JsonDerivedType(typeof(ReportNodeIndex), nameof(ReportNodeIndex))]
[JsonDerivedType(typeof(ReportNodeChart), nameof(ReportNodeChart))]
[JsonDerivedType(typeof(ReportNodeValuation), nameof(ReportNodeValuation))]
[JsonDerivedType(typeof(ReportNodeTransactions), nameof(ReportNodeTransactions))]
[JsonDerivedType(typeof(ReportNodeCash), nameof(ReportNodeCash))]
public abstract record ReportNodeBase(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title);

[JsonConverter(typeof(JsonStringEnumConverter<ReportValuationColumnKey>))]
public enum ReportValuationColumnKey
{
    InstrumentName,
    ISIN,
    Sedol,
    QuotePrice,
    BookValue,
    BookCost,
    Weight,
    Target,
    Min,
    Max
}

public sealed record ReportValuationColumn(
    ReportValuationColumnKey ColumnKey,
    int DisplayOrder);

public sealed record ReportNodeCoverPage(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title);

public sealed record ReportNodeIndex(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title);

public sealed record ReportNodeChart(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title,
    AssetAllocationID AssetAllocationID,
    ReportChartType ChartType) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title);

public sealed record ReportNodeValuation(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title,
    AssetAllocationID AssetAllocationID,
    List<ReportValuationColumn>? Columns = null) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title);

public sealed record ReportNodeTransactions(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title,
    AssetAllocationID AssetAllocationID) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title);

public sealed record ReportNodeCash(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title,
    AssetAllocationID AssetAllocationID) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title);
