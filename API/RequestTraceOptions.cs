using Repository;

namespace API;

public sealed class RequestTraceOptions
{
    public const string SectionName = "RequestTrace";

    public bool Enabled { get; init; } = true;

    public bool CaptureApi { get; init; } = true;

    public bool CaptureBodies { get; init; } = true;

    public bool Capture500StackTraces { get; init; } = true;

    public bool CaptureLogMessages { get; init; }

    public string MinimumLogLevel { get; init; } = "Warning";

    public int MaximumBodyCharacters { get; init; } = 32_000;

    public int QueueCapacity { get; init; } = 4_096;

    public int BatchSize { get; init; } = 100;

    public string[] CapturedContentTypePrefixes { get; init; } =
    [
        "application/json",
        "application/problem+json",
        "text/"
    ];

    public string[] ExcludedPathPrefixes { get; init; } =
    [
        "/Auth/Session",
        "/Diagnostics/RequestTrace",
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

    public RequestTraceSettings ToSettings() =>
        new()
        {
            Enabled = Enabled,
            CaptureApi = CaptureApi,
            CaptureBodies = CaptureBodies,
            Capture500StackTraces = Capture500StackTraces,
            CaptureLogMessages = CaptureLogMessages,
            MinimumLogLevel = string.IsNullOrWhiteSpace(MinimumLogLevel) ? "Warning" : MinimumLogLevel,
            MaximumBodyCharacters = MaximumBodyCharacters <= 0 ? 32_000 : MaximumBodyCharacters,
            CapturedContentTypePrefixes = CapturedContentTypePrefixes.Length == 0 ? new RequestTraceSettings().CapturedContentTypePrefixes : CapturedContentTypePrefixes,
            ExcludedPathPrefixes = ExcludedPathPrefixes.Length == 0 ? new RequestTraceSettings().ExcludedPathPrefixes : ExcludedPathPrefixes,
            RedactedHeaders = RedactedHeaders.Length == 0 ? new RequestTraceSettings().RedactedHeaders : RedactedHeaders
        };
}
