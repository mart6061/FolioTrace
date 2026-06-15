using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportNodeIndex(
    ReportNodeID ReportNodeID,
    int DisplayOrder,
    string Name,
    string Title) : ReportNodeBase(ReportNodeID, DisplayOrder, Name, Title);
