namespace API;

public sealed record MemoryDiagnosticsResponse(EventCacheDiagnosticsResponse EventCache, CountryServiceDiagnosticsResponse CountryService);

public sealed record EventCacheDiagnosticsResponse(bool IsLoaded, int StreamCount, int EventCount);

public sealed record CountryServiceDiagnosticsResponse(int CacheEntryCount, int CountryCount);
