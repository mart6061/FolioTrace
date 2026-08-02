namespace Services;

public sealed record ProfitLossServiceDiagnostics(
    int CacheEntryCount,
    int HoldingCount,
    long EstimatedMemoryBytes,
    int SnapshotVerifiedCount,
    int SnapshotMismatchCount,
    DateTime? LastSnapshotMismatchAtUtc,
    string? LastSnapshotMismatchDetails);
