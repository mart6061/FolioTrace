namespace Repository;

public static class RequestTraceConstants
{
    public const string RequestIdHeader = "X-FolioTrace-Request-Id";

    public const string HttpContextRequestIdItem = "FolioTrace.RequestTrace.RequestId";

    public static readonly Guid SettingsDocumentId = Guid.Parse("01900000-0000-7000-8000-000000000101");
}

public sealed record RequestTraceEvent
{
    public Guid Id { get; init; } = Guid.CreateGuid7();

    public Guid RequestId { get; init; }

    public string Source { get; init; } = RequestTraceSources.Api;

    public string Kind { get; init; } = RequestTraceEventKinds.Request;

    public DateTime RecordedAtUtc { get; init; } = DateTime.UtcNow;

    public DateTime? StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public long? DurationMilliseconds { get; init; }

    public string Method { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public string QueryString { get; init; } = string.Empty;

    public int? StatusCode { get; init; }

    public TraceHttpMessage? Message { get; init; }

    public string? ExceptionType { get; init; }

    public string? ExceptionMessage { get; init; }

    public string? StackTrace { get; init; }

    public string? LogLevel { get; init; }

    public string? LogCategory { get; init; }

    public string? LogEventId { get; init; }

    public string? LogMessage { get; init; }
}

public static class RequestTraceSources
{
    public const string Api = "API";

    public const string Ui = "UI";
}

public static class RequestTraceEventKinds
{
    public const string Request = "Request";

    public const string Response = "Response";

    public const string Exception = "Exception";

    public const string Log = "Log";
}

public sealed record TraceHttpMessage
{
    public Dictionary<string, string[]> Headers { get; init; } = [];

    public string? Body { get; init; }

    public string? ContentType { get; init; }

    public long? ContentLength { get; init; }

    public bool BodyTruncated { get; init; }
}

public sealed record RequestTrace
{
    public Guid RequestId { get; init; }

    public string Source { get; init; } = string.Empty;

    public DateTime StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public long? DurationMilliseconds { get; init; }

    public string Method { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public string QueryString { get; init; } = string.Empty;

    public int? StatusCode { get; init; }

    public bool HasResponse { get; init; }

    public bool HasException { get; init; }

    public int LogCount { get; init; }

    public TraceHttpMessage? Request { get; init; }

    public TraceHttpMessage? Response { get; init; }

    public RequestTraceException? Exception { get; init; }

    public IReadOnlyList<TraceLogEntry> Logs { get; init; } = [];
}

public sealed record RequestTraceException(
    DateTime RecordedAtUtc,
    string? ExceptionType,
    string? ExceptionMessage,
    string? StackTrace);

public sealed record TraceLogEntry(
    DateTime RecordedAtUtc,
    string Level,
    string Category,
    string? EventId,
    string Message,
    string? ExceptionType,
    string? ExceptionMessage,
    string? StackTrace);

public sealed record RequestTraceSettings
{
    public bool Enabled { get; init; } = true;

    public bool CaptureApi { get; init; } = true;

    public bool CaptureUi { get; init; } = true;

    public bool CaptureBodies { get; init; } = true;

    public bool Capture500StackTraces { get; init; } = true;

    public bool CaptureLogMessages { get; init; }

    public string MinimumLogLevel { get; init; } = "Warning";

    public int MaximumBodyCharacters { get; init; } = 32_000;

    public string[] CapturedContentTypePrefixes { get; init; } =
    [
        "application/json",
        "application/problem+json",
        "text/"
    ];

    public string[] ExcludedPathPrefixes { get; init; } =
    [
        "/Diagnostics/RequestTrace/Events",
        "/openapi",
        "/swagger"
    ];

    public string[] RedactedHeaders { get; init; } =
    [
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key"
    ];
}

public sealed record RequestTraceSettingsDocument
{
    public Guid Id { get; init; } = RequestTraceConstants.SettingsDocumentId;

    public RequestTraceSettings Settings { get; init; } = new();

    public DateTime ModifiedAtUtc { get; init; } = DateTime.UtcNow;
}
