using System.Diagnostics;
using System.Text;
using Repository;

namespace API;

public sealed class RequestTraceCaptureMiddleware(
    RequestDelegate next,
    ILogger<RequestTraceCaptureMiddleware> logger,
    RequestTraceSettingsService settingsService)
{
    public async Task InvokeAsync(HttpContext context, IRequestTraceRepository repository)
    {
        var settings = await settingsService.GetAsync(context.RequestAborted);

        if (!ShouldCapture(context, settings))
        {
            await next(context);
            return;
        }

        var requestId = ResolveRequestId(context);
        var startedAtUtc = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var originalResponseBody = context.Response.Body;
        var requestBody = await CaptureRequestBodyAsync(context.Request, settings);

        await AppendAsync(
            repository,
            new RequestTraceEvent
            {
                RequestId = requestId,
                Source = RequestTraceSources.Api,
                Kind = RequestTraceEventKinds.Request,
                RecordedAtUtc = startedAtUtc,
                StartedAtUtc = startedAtUtc,
                Method = context.Request.Method,
                Path = context.Request.Path.Value ?? string.Empty,
                QueryString = context.Request.QueryString.Value ?? string.Empty,
                Message = new TraceHttpMessage
                {
                    Headers = CaptureHeaders(context.Request.Headers, settings),
                    Body = requestBody.Body,
                    ContentType = context.Request.ContentType,
                    ContentLength = context.Request.ContentLength,
                    BodyTruncated = requestBody.Truncated
                }
            });

        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        using var _ = RequestTraceLogContext.Begin(requestId, RequestTraceSources.Api);

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var completedAtUtc = DateTime.UtcNow;
            var responseMessage = await CaptureResponseBodyAsync(context.Response, responseBody, settings);

            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalResponseBody, context.RequestAborted);
            context.Response.Body = originalResponseBody;

            await AppendAsync(
                repository,
                new RequestTraceEvent
                {
                    RequestId = requestId,
                    Source = RequestTraceSources.Api,
                    Kind = RequestTraceEventKinds.Response,
                    RecordedAtUtc = completedAtUtc,
                    CompletedAtUtc = completedAtUtc,
                    DurationMilliseconds = stopwatch.ElapsedMilliseconds,
                    Method = context.Request.Method,
                    Path = context.Request.Path.Value ?? string.Empty,
                    QueryString = context.Request.QueryString.Value ?? string.Empty,
                    StatusCode = context.Response.StatusCode,
                    Message = responseMessage
                });
        }
    }

    private static Guid ResolveRequestId(HttpContext context)
    {
        if (!Guid.TryParse(context.Request.Headers[RequestTraceConstants.RequestIdHeader].FirstOrDefault(), out var requestId))
            requestId = Guid.CreateGuid7();

        context.Items[RequestTraceConstants.HttpContextRequestIdItem] = requestId;
        context.Response.Headers[RequestTraceConstants.RequestIdHeader] = requestId.ToString();

        return requestId;
    }

    private async Task AppendAsync(IRequestTraceRepository repository, RequestTraceEvent traceEvent)
    {
        try
        {
            await repository.AppendAsync(traceEvent, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to persist request trace {Kind} event for {Method} {Path}.", traceEvent.Kind, traceEvent.Method, traceEvent.Path);
        }
    }

    private static bool ShouldCapture(HttpContext context, RequestTraceSettings settings)
    {
        if (!settings.Enabled || !settings.CaptureApi)
            return false;

        if (!context.Request.PathBase.Equals("/API", StringComparison.OrdinalIgnoreCase))
            return false;

        var path = context.Request.Path.Value ?? string.Empty;

        return !settings.ExcludedPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<CapturedBody> CaptureRequestBodyAsync(HttpRequest request, RequestTraceSettings settings)
    {
        if (!settings.CaptureBodies || !ShouldCaptureBody(request.ContentType, settings) || request.ContentLength is 0)
            return new CapturedBody(null, false);

        request.EnableBuffering();

        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return TrimBody(body, settings);
    }

    private static async Task<TraceHttpMessage> CaptureResponseBodyAsync(HttpResponse response, MemoryStream responseBody, RequestTraceSettings settings)
    {
        responseBody.Position = 0;
        var contentLength = responseBody.Length;
        CapturedBody capturedBody;

        if (settings.CaptureBodies && ShouldCaptureBody(response.ContentType, settings) && contentLength > 0)
        {
            using var reader = new StreamReader(responseBody, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            capturedBody = TrimBody(await reader.ReadToEndAsync(), settings);
        }
        else
        {
            capturedBody = new CapturedBody(null, false);
        }

        return new TraceHttpMessage
        {
            Headers = CaptureHeaders(response.Headers, settings),
            Body = capturedBody.Body,
            ContentType = response.ContentType,
            ContentLength = response.ContentLength ?? contentLength,
            BodyTruncated = capturedBody.Truncated
        };
    }

    private static Dictionary<string, string[]> CaptureHeaders(IHeaderDictionary headers, RequestTraceSettings settings) =>
        headers.ToDictionary(
            header => header.Key,
            header => settings.RedactedHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase)
                ? ["[redacted]"]
                : header.Value.Where(value => value is not null).Select(value => value!).ToArray(),
            StringComparer.OrdinalIgnoreCase);

    private static bool ShouldCaptureBody(string? contentType, RequestTraceSettings settings) =>
        !string.IsNullOrWhiteSpace(contentType) &&
        settings.CapturedContentTypePrefixes.Any(prefix => contentType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static CapturedBody TrimBody(string body, RequestTraceSettings settings) =>
        body.Length <= settings.MaximumBodyCharacters
            ? new CapturedBody(body, false)
            : new CapturedBody(body[..settings.MaximumBodyCharacters], true);

    private sealed record CapturedBody(string? Body, bool Truncated);
}
