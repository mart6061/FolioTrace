namespace Services;

public sealed record FXServiceDiagnostics(
    int CacheEntryCount,
    int FXCount,
    long EstimatedMemoryBytes);
