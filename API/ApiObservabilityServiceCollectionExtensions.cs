using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using SerilogOtlpProtocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol;

namespace API;

public static class ApiObservabilityServiceCollectionExtensions
{
    public static WebApplicationBuilder AddApiObservability(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration
            .GetSection(ApiObservabilityOptions.SectionName)
            .Get<ApiObservabilityOptions>() ?? new ApiObservabilityOptions();

        builder.Services.Configure<ApiObservabilityOptions>(builder.Configuration.GetSection(ApiObservabilityOptions.SectionName));
        builder.Services.AddHealthChecks();
        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console();

            if (options.OpenTelemetryEnabled && ShouldUseOtlpExporter(options))
            {
                loggerConfiguration.WriteTo.OpenTelemetry(otel =>
                {
                    if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                        otel.Endpoint = options.OtlpEndpoint;

                    otel.Protocol = ToSerilogOtlpProtocol(options.OtlpProtocol);
                    otel.ResourceAttributes = CreateResourceAttributes(options, builder.Environment);
                });
            }
        });

        if (!options.OpenTelemetryEnabled)
            return builder;

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => ConfigureResource(resource, options, builder.Environment))
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

        return builder;
    }

    private static ResourceBuilder ConfigureResource(ResourceBuilder resource, ApiObservabilityOptions options, IHostEnvironment environment) =>
        resource.AddService(
            serviceName: GetServiceName(options, environment),
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
            serviceInstanceId: Environment.MachineName);

    private static Dictionary<string, object> CreateResourceAttributes(ApiObservabilityOptions options, IHostEnvironment environment)
    {
        var attributes = new Dictionary<string, object>
        {
            ["service.name"] = GetServiceName(options, environment),
            ["service.instance.id"] = Environment.MachineName
        };

        var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString();

        if (!string.IsNullOrWhiteSpace(serviceVersion))
            attributes["service.version"] = serviceVersion;

        return attributes;
    }

    private static string GetServiceName(ApiObservabilityOptions options, IHostEnvironment environment) =>
        string.IsNullOrWhiteSpace(options.ServiceName) ? environment.ApplicationName : options.ServiceName;

    private static SerilogOtlpProtocol ToSerilogOtlpProtocol(OtlpExportProtocol protocol) =>
        protocol == OtlpExportProtocol.HttpProtobuf
            ? SerilogOtlpProtocol.HttpProtobuf
            : SerilogOtlpProtocol.Grpc;

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
