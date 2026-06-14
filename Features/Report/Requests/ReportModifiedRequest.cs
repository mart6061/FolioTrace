using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    EventDateTime EffectiveDateTime,
    string Reason,
    ReportID ReportID,
    string Name,
    bool Active,
    List<ReportNodeBase> Nodes) : IEventRequest;
