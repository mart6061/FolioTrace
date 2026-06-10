using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace API;

public sealed class ApiUnhandledExceptionLoggingMiddleware(
    RequestDelegate next,
    IWebHostEnvironment environment,
    ILogger<ApiUnhandledExceptionLoggingMiddleware> logger,
    ApiUnhandledExceptionLoggingOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var errorId = Guid.CreateGuid7();
            await WriteExceptionLogAsync(context, exception, errorId);
            var statusCode = exception switch
            {
                BadHttpRequestException badRequestException => badRequestException.StatusCode,
                ArgumentException argumentException when IsRequestValidationException(argumentException) => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            var title = statusCode == StatusCodes.Status400BadRequest
                ? "Invalid request."
                : "An unexpected error occurred.";
            logger.LogError(exception, "Unhandled API exception {ErrorId} for {Method} {Path}.", errorId, context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                type = $"https://httpstatuses.com/{statusCode}",
                title,
                status = statusCode,
                detail = statusCode == StatusCodes.Status400BadRequest ? exception.Message : null,
                traceId = context.TraceIdentifier,
                errorId
            }));
        }
    }

    private static bool IsRequestValidationException(ArgumentException exception) =>
        exception.Message.Contains("Value must not be in the future.", StringComparison.Ordinal);

    private async Task WriteExceptionLogAsync(HttpContext context, Exception exception, Guid errorId)
    {
        var logDirectory = Path.Combine(environment.ContentRootPath, options.LogDirectoryName);
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, $"api-errors-{DateTime.UtcNow:yyyyMMdd}.log");
        var logEntry = BuildLogEntry(context, exception, errorId);

        await File.AppendAllTextAsync(logPath, logEntry, Encoding.UTF8);
    }

    private static string BuildLogEntry(HttpContext context, Exception exception, Guid errorId)
    {
        var builder = new StringBuilder();

        builder.AppendLine("--------------------------------------------------------------------------------");
        builder.AppendLine($"ErrorId: {errorId}");
        builder.AppendLine($"Utc: {DateTime.UtcNow:O}");
        builder.AppendLine($"TraceId: {context.TraceIdentifier}");
        builder.AppendLine($"ActivityTraceId: {Activity.Current?.TraceId}");
        builder.AppendLine($"ActivitySpanId: {Activity.Current?.SpanId}");
        builder.AppendLine($"Request: {context.Request.Method} {context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}");
        builder.AppendLine($"RemoteIpAddress: {context.Connection.RemoteIpAddress}");
        builder.AppendLine($"UserAgent: {context.Request.Headers.UserAgent}");
        builder.AppendLine("Exception:");
        builder.AppendLine(exception.ToString());
        builder.AppendLine();

        return builder.ToString();
    }
}
