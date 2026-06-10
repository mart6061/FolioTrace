using System.Diagnostics;

namespace API;

public sealed class ApiRequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<ApiRequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        var tags = CreateTags(context);

        ApiObservability.RequestsStarted.Add(1, tags);

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceIdentifier"] = context.TraceIdentifier,
            ["TraceId"] = Activity.Current?.TraceId.ToString(),
            ["SpanId"] = Activity.Current?.SpanId.ToString()
        });

        try
        {
            await next(context);
        }
        finally
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            var completedTags = CreateTags(context);

            ApiObservability.RequestsCompleted.Add(1, completedTags);
            ApiObservability.RequestDuration.Record(elapsedMilliseconds, completedTags);

            LogRequestCompleted(context, elapsedMilliseconds);
        }
    }

    private static TagList CreateTags(HttpContext context)
    {
        var route = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "/";

        return new TagList
        {
            { "http.request.method", context.Request.Method },
            { "http.route", route },
            { "http.response.status_code", context.Response.StatusCode }
        };
    }

    private void LogRequestCompleted(HttpContext context, double elapsedMilliseconds)
    {
        var statusCode = context.Response.StatusCode;
        var level = statusCode >= StatusCodes.Status500InternalServerError
            ? LogLevel.Error
            : statusCode >= StatusCodes.Status400BadRequest
                ? LogLevel.Warning
                : LogLevel.Information;

        logger.Log(
            level,
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds:0.000} ms.",
            context.Request.Method,
            context.Request.PathBase.Add(context.Request.Path),
            statusCode,
            elapsedMilliseconds);
    }
}
