using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportCreatedRequest(
    UserID UserID,
    ReportID? ReportID,
    string Name,
    bool Active,
    List<ReportNodeBase> Nodes) : IConfigEventRequest;
