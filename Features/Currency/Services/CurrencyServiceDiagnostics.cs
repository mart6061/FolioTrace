namespace Services;

public sealed record CurrencyServiceDiagnostics(
    int CacheEntryCount,
    int CurrencyCount,
    long EstimatedMemoryBytes);
