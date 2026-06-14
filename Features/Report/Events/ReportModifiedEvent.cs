using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Report Modified Event")]
public sealed record ReportModifiedEvent : EventBase, IReportEvent
{
    [EventProperty(Description = "Report ID")]
    public ReportID ReportID { get; init; } = null!;

    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;

    [EventProperty(Description = "Active")]
    public bool Active { get; init; }

    [EventProperty(Description = "Effective Date Time")]
    public EventDateTime EffectiveDateTime { get; init; } = null!;

    [EventProperty(Description = "Nodes")]
    public List<ReportNodeBase> Nodes { get; init; } = [];

    [JsonConstructor]
    private ReportModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal ReportModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, ReportID reportID, string name, bool active, EventDateTime effectiveDateTime, List<ReportNodeBase> nodes)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        ReportID = reportID;
        Name = name?.Trim() ?? string.Empty;
        Active = active;
        EffectiveDateTime = ReportConfigBuilder.DateOnly(effectiveDateTime);
        Nodes = ReportConfigBuilder.NormaliseNodes(nodes);
    }

    public override string Type => nameof(ReportModifiedEvent);
}
