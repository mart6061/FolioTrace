using System.Diagnostics;
using System.Text;
using Repository;

namespace API;

public sealed class RequestTraceCaptureMiddleware(
    RequestDelegate next,
    ILogger<RequestTraceCaptureMiddleware> logger,
    RequestTraceSettingsService settingsService,
    RequestTraceLogQueue queue)
{
    public async Task InvokeAsync(HttpContext context)
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

        Enqueue(
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

        var captureResponseBody = settings.CaptureBodies &&
            context.GetEndpoint()?.Metadata.GetMetadata<DisableRequestTraceBodyCaptureAttribute>() is null;
        var responseBody = captureResponseBody
            ? new ForwardingCaptureStream(originalResponseBody, settings.MaximumBodyCharacters * 4)
            : null;
        if (responseBody is not null)
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
            var responseMessage = CaptureResponseBody(context.Response, responseBody, settings);
            context.Response.Body = originalResponseBody;
            responseBody?.Dispose();

            Enqueue(
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

    private void Enqueue(RequestTraceEvent traceEvent)
    {
        if (!queue.TryEnqueue(traceEvent))
            logger.LogWarning("Request trace queue rejected {Kind} event for {Method} {Path}.", traceEvent.Kind, traceEvent.Method, traceEvent.Path);
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

    private static TraceHttpMessage CaptureResponseBody(HttpResponse response, ForwardingCaptureStream? responseBody, RequestTraceSettings settings)
    {
        var contentLength = responseBody?.TotalBytes ?? response.ContentLength ?? 0;
        CapturedBody capturedBody;

        if (responseBody is not null && ShouldCaptureBody(response.ContentType, settings) && contentLength > 0)
        {
            var body = Encoding.UTF8.GetString(responseBody.CapturedBytes.Span);
            capturedBody = TrimBody(body, settings) with { Truncated = responseBody.IsTruncated || body.Length > settings.MaximumBodyCharacters };
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
