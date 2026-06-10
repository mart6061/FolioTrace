using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace API;

public static class ApiObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddApiObservability(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var options = configuration
            .GetSection(ApiObservabilityOptions.SectionName)
            .Get<ApiObservabilityOptions>() ?? new ApiObservabilityOptions();

        services.Configure<ApiObservabilityOptions>(configuration.GetSection(ApiObservabilityOptions.SectionName));
        services.AddHealthChecks();

        if (!options.OpenTelemetryEnabled)
            return services;

        var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString();

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: string.IsNullOrWhiteSpace(options.ServiceName) ? environment.ApplicationName : options.ServiceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ApiObservability.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.RecordException = true;
                    })
                    .AddHttpClientInstrumentation();

                if (ShouldUseOtlpExporter(options))
                    tracing.AddOtlpExporter(exporter => ConfigureOtlpExporter(exporter, options));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(ApiObservability.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (ShouldUseOtlpExporter(options))
                    metrics.AddOtlpExporter(exporter => ConfigureOtlpExporter(exporter, options));
            });

        return services;
    }

    private static bool ShouldUseOtlpExporter(ApiObservabilityOptions options) =>
        options.OtlpExporterEnabled ||
        !string.IsNullOrWhiteSpace(options.OtlpEndpoint) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

    private static void ConfigureOtlpExporter(OtlpExporterOptions exporter, ApiObservabilityOptions options)
    {
        exporter.Protocol = options.OtlpProtocol;

        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
            exporter.Endpoint = new Uri(options.OtlpEndpoint, UriKind.Absolute);
    }
}
