using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportConfig : IType
{
    public required ReportID ReportID { get; init; }

    public required string Name { get; init; }

    public required bool Active { get; init; }

    public required List<ReportNodeBase> Nodes { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required EventID LastEventID { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public ReportConfig(ReportID reportID, string name, bool active, List<ReportNodeBase> nodes, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        ReportID = reportID;
        Name = name;
        Active = active;
        Nodes = ReportConfigBuilder.CloneNodes(nodes);
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public ReportConfig(ReportID reportID, string name, bool active, List<ReportNodeBase> nodes, AuditDateTime auditDateTime, EventID lastEventID)
        : this(reportID, name, active, nodes, auditDateTime, lastEventID, new LastAuditDateTime(auditDateTime.Value))
    {
    }
}
