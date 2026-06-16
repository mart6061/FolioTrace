using FolioTrace.Types;

namespace FolioTrace.Common;

public interface IAuditEventBase : IType
{
    [EventProperty(Description = "Event type", Order = 0)]
    string Type { get; }

    [EventProperty(Description = "Event ID", Order = 10)]
    EventID EventID { get; }

    [EventProperty(Description = "User ID", Order = 20)]
    UserID UserID { get; }

    [EventProperty(Description = "Audit time", Order = 40)]
    AuditDateTime AuditDateTime { get; }
}
