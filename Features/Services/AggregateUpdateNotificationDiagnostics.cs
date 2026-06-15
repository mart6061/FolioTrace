using System.Collections.Concurrent;
using System.Threading.Channels;
using FolioTrace.Aggregates;
using FolioTrace.Common;

namespace Services;

public sealed record AggregateUpdateNotificationDiagnostics(
    int ActiveSubscriberCount,
    long PublishedNotificationCount,
    string? LastNotificationType,
    string? LastKind,
    Guid? LastEventID,
    DateTime? LastEventDateTime,
    DateTime? LastAuditDateTime,
    string? LastReason,
    Guid? CurrentBuildID,
    string? LastBuildStatus,
    string? LastBuildStage,
    DateTime? LastBuildUpdatedAtUtc);
