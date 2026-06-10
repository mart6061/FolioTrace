namespace API;

public static class ApiRequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseApiRequestLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<ApiRequestLoggingMiddleware>();
        return app;
    }
}
