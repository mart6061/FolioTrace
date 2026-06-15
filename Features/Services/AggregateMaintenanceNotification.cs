using System.Collections.Concurrent;
using System.Threading.Channels;
using FolioTrace.Aggregates;
using FolioTrace.Common;

namespace Services;

public sealed record AggregateMaintenanceNotification(
    string NotificationType,
    string Status,
    string? Trigger,
    bool Changed,
    DateTime UpdatedAtUtc);
