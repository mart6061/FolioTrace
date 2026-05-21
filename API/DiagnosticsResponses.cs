namespace API;

public sealed record MemoryDiagnosticsResponse(EventCacheDiagnosticsResponse EventCache, CountryServiceDiagnosticsResponse CountryService, CurrencyServiceDiagnosticsResponse CurrencyService, FXServiceDiagnosticsResponse FXService, FXRateServiceDiagnosticsResponse FXRateService);

public sealed record EventCacheDiagnosticsResponse(bool IsLoaded, int StreamCount, int EventCount);

public sealed record CountryServiceDiagnosticsResponse(int CacheEntryCount, int CountryCount);

public sealed record CurrencyServiceDiagnosticsResponse(int CacheEntryCount, int CurrencyCount);

public sealed record FXServiceDiagnosticsResponse(int CacheEntryCount, int FXCount);

public sealed record FXRateServiceDiagnosticsResponse(int CacheEntryCount, int FXRateCount);

public sealed record ApiExchangeSearchResponse(
    IReadOnlyList<ApiExchangeResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ApiExchangeResponse(
    Guid Id,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    long DurationMilliseconds,
    string Method,
    string Path,
    string QueryString,
    int? StatusCode,
    string? ExceptionType,
    string? ExceptionMessage,
    ApiHttpMessageResponse Request,
    ApiHttpMessageResponse Response);

public sealed record ApiHttpMessageResponse(
    Dictionary<string, string[]> Headers,
    string? Body,
    string? ContentType,
    long? ContentLength,
    bool BodyTruncated);
