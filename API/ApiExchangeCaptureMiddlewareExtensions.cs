namespace API;

public static class ApiExchangeCaptureMiddlewareExtensions
{
    public static IApplicationBuilder UseApiExchangeCapture(this IApplicationBuilder app)
    {
        app.UseMiddleware<ApiExchangeCaptureMiddleware>(new ApiExchangeCaptureOptions());
        return app;
    }
}
