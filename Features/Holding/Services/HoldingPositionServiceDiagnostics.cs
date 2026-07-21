using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed record HoldingPositionServiceDiagnostics(
    int CacheEntryCount,
    int PositionCount,
    long EstimatedMemoryBytes,
    int SnapshotVerifiedCount,
    int SnapshotMismatchCount,
    DateTime? LastSnapshotMismatchAtUtc,
    string? LastSnapshotMismatchDetails);
