using FolioTrace.Common;

namespace Repository;

public sealed record StoredEvent(Guid StreamId, IAuditEventBase Event);
