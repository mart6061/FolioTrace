namespace Services;

public sealed record BrokerServiceDiagnostics(
    int CacheEntryCount,
    int BrokerCount,
    long EstimatedMemoryBytes);
