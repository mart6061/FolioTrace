namespace Services;

public sealed record InputControlSettingsServiceDiagnostics(
    int CacheEntryCount,
    int SettingCount,
    long EstimatedMemoryBytes);
