namespace Services;

public sealed record ValuationSettingServiceDiagnostics(
    int CacheEntryCount,
    int ValuationSettingCount,
    long EstimatedMemoryBytes);
