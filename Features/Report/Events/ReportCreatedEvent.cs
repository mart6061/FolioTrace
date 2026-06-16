using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "Report Created Event")]
public sealed record ReportCreatedEvent : ConfigEventBase, IReportEvent
{
    [EventProperty(Description = "Report ID")]
    public ReportID ReportID { get; init; } = null!;

    [EventProperty(Description = "Name")]
    public string Name { get; init; } = string.Empty;

    [EventProperty(Description = "Active")]
    public bool Active { get; init; }

    [EventProperty(Description = "Nodes")]
    public List<ReportNodeBase> Nodes { get; init; } = [];

    [JsonConstructor]
    private ReportCreatedEvent()
        : base(null!, null!, null!)
    {
    }

    internal ReportCreatedEvent(EventID eventId, UserID userId, AuditDateTime auditDateTime, ReportID reportID, string name, bool active, List<ReportNodeBase> nodes)
        : base(eventId, userId, auditDateTime)
    {
        ReportID = reportID;
        Name = name?.Trim() ?? string.Empty;
        Active = active;
        Nodes = ReportConfigBuilder.NormaliseNodes(nodes);
    }

    public override string Type => nameof(ReportCreatedEvent);
}
