using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportModifiedRequest(
    UserID UserID,
    ReportID ReportID,
    string Name,
    bool Active,
    List<ReportNodeBase> Nodes) : IConfigEventRequest;
