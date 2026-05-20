namespace API;

public sealed class ApiExchangeCaptureOptions
{
    public int MaximumBodyCharacters { get; init; } = 32_000;

    public string[] CapturedContentTypePrefixes { get; init; } =
    [
        "application/json",
        "application/problem+json",
        "text/"
    ];

    public string[] ExcludedPathPrefixes { get; init; } =
    [
        "/Diagnostics/HttpExchanges",
        "/openapi",
        "/scalar"
    ];

    public string[] RedactedHeaders { get; init; } =
    [
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key"
    ];
}
