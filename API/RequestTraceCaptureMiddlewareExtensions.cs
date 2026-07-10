namespace API;

public static class RequestTraceCaptureMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTraceCapture(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestTraceCaptureMiddleware>();
        return app;
    }
}
