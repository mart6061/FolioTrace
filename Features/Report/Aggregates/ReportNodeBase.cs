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
    string Title)
{
    public ReportNodePageOrientation PageOrientation { get; init; } = ReportNodePageOrientation.Portrait;
}
