using Repository;
using Serilog.Core;
using Serilog.Events;

namespace API;

public sealed class RequestTraceSerilogSink(IServiceProvider serviceProvider) : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        var scope = RequestTraceLogContext.Current;
        if (scope is null)
            return;

        var settingsService = serviceProvider.GetService<RequestTraceSettingsService>();
        var queue = serviceProvider.GetService<RequestTraceLogQueue>();
        if (settingsService is null || queue is null)
            return;

        var settings = settingsService.Current;
        if (!settings.Enabled || !settings.CaptureApi || !settings.CaptureLogMessages)
            return;

        if (!MeetsMinimumLevel(logEvent.Level, settings.MinimumLogLevel))
            return;

        var category = GetScalarProperty(logEvent, "SourceContext") ?? string.Empty;
        if (IsExcludedCategory(category))
            return;

        queue.TryEnqueue(new RequestTraceEvent
        {
            RequestId = scope.RequestId,
            Source = scope.Source,
            Kind = RequestTraceEventKinds.Log,
            RecordedAtUtc = logEvent.Timestamp.UtcDateTime,
            LogLevel = logEvent.Level.ToString(),
            LogCategory = category,
            LogEventId = GetEventId(logEvent),
            LogMessage = logEvent.RenderMessage(),
            ExceptionType = logEvent.Exception?.GetType().FullName,
            ExceptionMessage = logEvent.Exception?.Message,
            StackTrace = settings.Capture500StackTraces ? logEvent.Exception?.ToString() : null
        });
    }

    private static bool IsExcludedCategory(string category) =>
        category.Contains("RequestTrace", StringComparison.OrdinalIgnoreCase) ||
        category.Contains("MartenRequestTraceRepository", StringComparison.OrdinalIgnoreCase);

    private static string? GetEventId(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("EventId", out var eventId))
            return null;

        return eventId.ToString().Trim('"');
    }

    private static string? GetScalarProperty(LogEvent logEvent, string propertyName)
    {
        if (!logEvent.Properties.TryGetValue(propertyName, out var value))
            return null;

        return value is ScalarValue { Value: string text } ? text : value.ToString().Trim('"');
    }

    private static bool MeetsMinimumLevel(LogEventLevel level, string configuredMinimum)
    {
        var minimum = configuredMinimum.Trim().ToLowerInvariant() switch
        {
            "verbose" or "trace" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" or "info" => LogEventLevel.Information,
            "warning" or "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" or "critical" => LogEventLevel.Fatal,
            _ => LogEventLevel.Warning
        };

        return level >= minimum;
    }
}
