namespace API;

public static class ApiUnhandledExceptionLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseApiUnhandledExceptionLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<ApiUnhandledExceptionLoggingMiddleware>(new ApiUnhandledExceptionLoggingOptions());
        return app;
    }
}
