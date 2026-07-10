namespace API;

public sealed record MemoryDiagnosticsResponse(EventCacheDiagnosticsResponse EventCache, AccountServiceDiagnosticsResponse AccountService, BrokerServiceDiagnosticsResponse BrokerService, CountryServiceDiagnosticsResponse CountryService, CurrencyServiceDiagnosticsResponse CurrencyService, FXServiceDiagnosticsResponse FXService, FXRateServiceDiagnosticsResponse FXRateService, HoldingServiceDiagnosticsResponse HoldingService, HoldingPositionServiceDiagnosticsResponse HoldingPositionService, InstrumentServiceDiagnosticsResponse InstrumentService, InstrumentValueServiceDiagnosticsResponse InstrumentValueService, UserServiceDiagnosticsResponse UserService, SseDiagnosticsResponse Sse, AggregateMaintenanceDiagnosticsResponse AggregateMaintenance);

public sealed record EventCacheDiagnosticsResponse(bool IsLoaded, int StreamCount, int EventCount, long EstimatedMemoryBytes, int UnprocessedEventCount, IReadOnlyList<UnprocessedEventDiagnosticsResponse> RecentUnprocessedEvents);

public sealed record UnprocessedEventDiagnosticsResponse(Guid? StreamId, Guid? EventId, string EventType, string Reason, DateTime RecordedAtUtc);

public sealed record CountryServiceDiagnosticsResponse(int CacheEntryCount, int CountryCount, long EstimatedMemoryBytes);

public sealed record AccountServiceDiagnosticsResponse(int CacheEntryCount, int AccountCount, long EstimatedMemoryBytes);

public sealed record BrokerServiceDiagnosticsResponse(int CacheEntryCount, int BrokerCount, long EstimatedMemoryBytes);

public sealed record CurrencyServiceDiagnosticsResponse(int CacheEntryCount, int CurrencyCount, long EstimatedMemoryBytes);

public sealed record FXServiceDiagnosticsResponse(int CacheEntryCount, int FXCount, long EstimatedMemoryBytes);

public sealed record FXRateServiceDiagnosticsResponse(int CacheEntryCount, int FXRateCount, long EstimatedMemoryBytes);

public sealed record HoldingServiceDiagnosticsResponse(int CacheEntryCount, int HoldingCount, long EstimatedMemoryBytes);

public sealed record HoldingPositionServiceDiagnosticsResponse(int CacheEntryCount, int PositionCount, long EstimatedMemoryBytes);

public sealed record InstrumentServiceDiagnosticsResponse(int CacheEntryCount, int InstrumentCount, long EstimatedMemoryBytes);

public sealed record InstrumentValueServiceDiagnosticsResponse(int CacheEntryCount, int InstrumentValueCount, long EstimatedMemoryBytes);

public sealed record UserServiceDiagnosticsResponse(int CacheEntryCount, int UserCount, long EstimatedMemoryBytes);

public sealed record SseDiagnosticsResponse(
    int ActiveSubscriberCount,
    long PublishedNotificationCount,
    string? LastNotificationType,
    string? LastKind,
    Guid? LastEventID,
    DateTime? LastEventDateTime,
    DateTime? LastAuditDateTime,
    string? LastReason,
    Guid? CurrentBuildID,
    string? LastBuildStatus,
    string? LastBuildStage,
    DateTime? LastBuildUpdatedAtUtc);

public sealed record AggregateMaintenanceDiagnosticsResponse(
    bool Enabled,
    TimeSpan PeriodicDelay,
    int EventTriggerCount,
    TimeSpan EventTriggerDelay,
    int DaysFromToday,
    int EndOfWeeksFromToday,
    int EndOfMonthsFromToday,
    string Status,
    Guid? ActiveRunID,
    Guid? LastRunID,
    string? LastTrigger,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc,
    int LastScannedAggregates,
    int LastMissingAggregates,
    int LastFixedAggregates,
    int LastFailedAggregates,
    long TotalScannedAggregates,
    long TotalMissingAggregates,
    long TotalFixedAggregates,
    long TotalFailedAggregates,
    int SkippedRunCount,
    int PendingEventCount,
    bool IsSuspended,
    string? SuspensionReason,
    DateTime? SuspendedAtUtc,
    int SuspendedRunCount,
    string? LastError,
    IReadOnlyList<string> RecentErrors);

public sealed record RequestTraceSearchResponse(
    IReadOnlyList<RequestTraceResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    RequestTraceSettingsResponse Settings);

public sealed record RequestTraceResponse(
    Guid RequestId,
    string Source,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    long? DurationMilliseconds,
    string Method,
    string Path,
    string QueryString,
    int? StatusCode,
    bool HasResponse,
    bool HasException,
    int LogCount,
    TraceHttpMessageResponse? Request,
    TraceHttpMessageResponse? Response,
    RequestTraceExceptionResponse? Exception,
    IReadOnlyList<TraceLogEntryResponse> Logs);

public sealed record TraceHttpMessageResponse(
    Dictionary<string, string[]> Headers,
    string? Body,
    string? ContentType,
    long? ContentLength,
    bool BodyTruncated);

public sealed record RequestTraceExceptionResponse(
    DateTime RecordedAtUtc,
    string? ExceptionType,
    string? ExceptionMessage,
    string? StackTrace);

public sealed record TraceLogEntryResponse(
    DateTime RecordedAtUtc,
    string Level,
    string Category,
    string? EventId,
    string Message,
    string? ExceptionType,
    string? ExceptionMessage,
    string? StackTrace);

public sealed record RequestTraceSettingsResponse(
    bool Enabled,
    bool CaptureApi,
    bool CaptureUi,
    bool CaptureBodies,
    bool Capture500StackTraces,
    bool CaptureLogMessages,
    string MinimumLogLevel,
    int MaximumBodyCharacters,
    IReadOnlyList<string> CapturedContentTypePrefixes,
    IReadOnlyList<string> ExcludedPathPrefixes,
    IReadOnlyList<string> RedactedHeaders);

public sealed record RequestTraceSettingsRequest(
    bool Enabled,
    bool CaptureApi,
    bool CaptureUi,
    bool CaptureBodies,
    bool Capture500StackTraces,
    bool CaptureLogMessages,
    string MinimumLogLevel,
    int MaximumBodyCharacters,
    IReadOnlyList<string> CapturedContentTypePrefixes,
    IReadOnlyList<string> ExcludedPathPrefixes,
    IReadOnlyList<string> RedactedHeaders);

public sealed record RequestTracePurgeRequest(string Confirmation, DateTime? BeforeUtc);

public sealed record RequestTracePurgeResponse(int DeletedCount);

public sealed record RequestTraceEventIngestRequest(
    Guid RequestId,
    string Source,
    string Kind,
    DateTime RecordedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    long? DurationMilliseconds,
    string Method,
    string Path,
    string QueryString,
    int? StatusCode,
    TraceHttpMessageResponse? Message,
    string? ExceptionType,
    string? ExceptionMessage,
    string? StackTrace);

public sealed record FIXOperationSearchResponse(
    IReadOnlyList<FIXOperationResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record FIXOperationResponse(
    Guid EventID,
    DateTime RecordedAtUtc,
    DateTime EventDateTime,
    DateTime AuditDateTime,
    string Reason,
    string Direction,
    string Channel,
    string SessionID,
    string MsgType,
    string MessageName,
    int? MsgSeqNum,
    string SenderCompID,
    string TargetCompID,
    DateTime? SendingTime,
    string ClOrdID,
    string ExecID,
    string RawMessage,
    string DisplayMessage);
