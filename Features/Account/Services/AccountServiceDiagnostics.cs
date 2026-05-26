namespace Services;

public sealed record AccountServiceDiagnostics(
    int CacheEntryCount,
    int AccountCount,
    long EstimatedMemoryBytes);
