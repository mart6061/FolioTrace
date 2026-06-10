using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace API;

public static class ApiObservability
{
    public const string ActivitySourceName = "FolioTrace.API";
    public const string MeterName = "FolioTrace.API";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> RequestsStarted = Meter.CreateCounter<long>(
        "foliotrace.api.requests.started",
        unit: "{request}",
        description: "Number of API requests started.");

    public static readonly Counter<long> RequestsCompleted = Meter.CreateCounter<long>(
        "foliotrace.api.requests.completed",
        unit: "{request}",
        description: "Number of API requests completed.");

    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "foliotrace.api.request.duration",
        unit: "ms",
        description: "API request duration in milliseconds.");
}
