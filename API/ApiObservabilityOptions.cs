using OpenTelemetry.Exporter;

namespace API;

public sealed class ApiObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; init; } = "FolioTrace.API";

    public bool OpenTelemetryEnabled { get; init; } = true;

    public bool OtlpExporterEnabled { get; init; }

    public string? OtlpEndpoint { get; init; }

    public OtlpExportProtocol OtlpProtocol { get; init; } = OtlpExportProtocol.Grpc;
}
