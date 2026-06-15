using System.Collections.Concurrent;
using System.Threading.Channels;
using FolioTrace.Aggregates;
using FolioTrace.Common;

namespace Services;

public sealed record BuildProgressNotification(
    string NotificationType,
    Guid BuildID,
    string Status,
    string Stage,
    string Message,
    int CompletedSteps,
    int TotalSteps,
    int CompletedEvents,
    int TotalEvents,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc,
    string? Error);
