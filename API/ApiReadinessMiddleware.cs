using System.Text.Json;

namespace API;

public sealed class ApiReadinessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApiReadinessState readinessState)
    {
        if (readinessState.Ready || IsAvailableWhileNotReady(context.Request.Path))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            Ready = false,
            Status = "Starting",
            Message = "The API is loading events from the event store."
        }), context.RequestAborted);
    }

    private static bool IsAvailableWhileNotReady(PathString path) =>
        path.Equals("/System/Health", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/System/Build", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/Notifications", StringComparison.OrdinalIgnoreCase);
}
