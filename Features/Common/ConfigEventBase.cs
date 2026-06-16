using FolioTrace.Types;

namespace FolioTrace.Common;

public abstract record ConfigEventBase(EventID EventID, UserID UserID, AuditDateTime AuditDateTime)
    : AuditEventBase(EventID, UserID, AuditDateTime), IConfigEventBase;
