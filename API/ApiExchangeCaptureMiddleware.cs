using System.Diagnostics;
using System.Text;
using Repository;

namespace API;

public sealed class ApiExchangeCaptureMiddleware(
    RequestDelegate next,
    ILogger<ApiExchangeCaptureMiddleware> logger,
    ApiExchangeCaptureOptions options)
{
    public async Task InvokeAsync(HttpContext context, IApiExchangeRepository repository)
    {
        if (!ShouldCapture(context))
        {
            await next(context);
            return;
        }

        var startedAtUtc = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var originalResponseBody = context.Response.Body;
        var requestBody = await CaptureRequestBodyAsync(context.Request);

        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        Exception? exception = null;

        try
        {
            await next(context);
        }
        catch (Exception caughtException)
        {
            exception = caughtException;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var completedAtUtc = DateTime.UtcNow;
            var responseMessage = await CaptureResponseBodyAsync(context.Response, responseBody);

            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalResponseBody, context.RequestAborted);
            context.Response.Body = originalResponseBody;

            var exchange = new ApiExchange
            {
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                DurationMilliseconds = stopwatch.ElapsedMilliseconds,
                Method = context.Request.Method,
                Path = context.Request.Path.Value ?? string.Empty,
                QueryString = context.Request.QueryString.Value ?? string.Empty,
                StatusCode = context.Response.HasStarted ? context.Response.StatusCode : context.Response.StatusCode,
                ExceptionType = exception?.GetType().FullName,
                ExceptionMessage = exception?.Message,
                Request = new ApiHttpMessage
                {
                    Headers = CaptureHeaders(context.Request.Headers),
                    Body = requestBody.Body,
                    ContentType = context.Request.ContentType,
                    ContentLength = context.Request.ContentLength,
                    BodyTruncated = requestBody.Truncated
                },
                Response = responseMessage
            };

            try
            {
                await repository.StoreAsync(exchange, context.RequestAborted);
            }
            catch (Exception loggingException)
            {
                logger.LogWarning(loggingException, "Failed to persist API exchange capture for {Method} {Path}.", exchange.Method, exchange.Path);
            }
        }
    }

    private bool ShouldCapture(HttpContext context)
    {
        if (!context.Request.PathBase.Equals("/API", StringComparison.OrdinalIgnoreCase))
            return false;

        var path = context.Request.Path.Value ?? string.Empty;

        return !options.ExcludedPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<CapturedBody> CaptureRequestBodyAsync(HttpRequest request)
    {
        if (!ShouldCaptureBody(request.ContentType) || request.ContentLength is 0)
            return new CapturedBody(null, false);

        request.EnableBuffering();

        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return TrimBody(body);
    }

    private async Task<ApiHttpMessage> CaptureResponseBodyAsync(HttpResponse response, MemoryStream responseBody)
    {
        responseBody.Position = 0;
        var contentLength = responseBody.Length;
        CapturedBody capturedBody;

        if (ShouldCaptureBody(response.ContentType) && contentLength > 0)
        {
            using var reader = new StreamReader(responseBody, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            capturedBody = TrimBody(await reader.ReadToEndAsync());
        }
        else
        {
            capturedBody = new CapturedBody(null, false);
        }

        return new ApiHttpMessage
        {
            Headers = CaptureHeaders(response.Headers),
            Body = capturedBody.Body,
            ContentType = response.ContentType,
            ContentLength = response.ContentLength ?? contentLength,
            BodyTruncated = capturedBody.Truncated
        };
    }

    private Dictionary<string, string[]> CaptureHeaders(IHeaderDictionary headers) =>
        headers.ToDictionary(
            header => header.Key,
            header => options.RedactedHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase)
                ? ["[redacted]"]
                : header.Value.Where(value => value is not null).Select(value => value!).ToArray(),
            StringComparer.OrdinalIgnoreCase);

    private bool ShouldCaptureBody(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) &&
        options.CapturedContentTypePrefixes.Any(prefix => contentType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private CapturedBody TrimBody(string body) =>
        body.Length <= options.MaximumBodyCharacters
            ? new CapturedBody(body, false)
            : new CapturedBody(body[..options.MaximumBodyCharacters], true);

    private sealed record CapturedBody(string? Body, bool Truncated);
}
