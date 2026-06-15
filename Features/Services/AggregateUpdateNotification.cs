using System.Collections.Concurrent;
using System.Threading.Channels;
using FolioTrace.Aggregates;
using FolioTrace.Common;

namespace Services;

public sealed record AggregateUpdateNotification(
    string NotificationType,
    string Kind,
    Guid? EventID,
    DateTime? EventDateTime,
    DateTime? AuditDateTime,
    DateTime? AffectedFrom,
    string Reason);
