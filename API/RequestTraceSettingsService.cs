using Microsoft.Extensions.Options;
using Repository;

namespace API;

public sealed class RequestTraceSettingsService(
    IServiceScopeFactory scopeFactory,
    IOptions<RequestTraceOptions> options,
    ILogger<RequestTraceSettingsService> logger)
{
    private readonly SemaphoreSlim loadLock = new(1, 1);
    private volatile bool loaded;
    private RequestTraceSettings current = options.Value.ToSettings();

    public RequestTraceSettings Current => current;

    public async Task<RequestTraceSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        if (loaded)
            return current;

        await loadLock.WaitAsync(cancellationToken);
        try
        {
            if (loaded)
                return current;

            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRequestTraceRepository>();
            var stored = await repository.LoadSettingsAsync(cancellationToken);

            current = Sanitize(stored ?? options.Value.ToSettings());
            loaded = true;

            return current;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to load request trace runtime settings. Falling back to configured defaults.");
            loaded = true;
            return current;
        }
        finally
        {
            loadLock.Release();
        }
    }

    public async Task<RequestTraceSettings> UpdateAsync(RequestTraceSettings settings, CancellationToken cancellationToken = default)
    {
        var sanitized = Sanitize(settings);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRequestTraceRepository>();
        await repository.StoreSettingsAsync(sanitized, cancellationToken);

        current = sanitized;
        loaded = true;

        return sanitized;
    }

    private static RequestTraceSettings Sanitize(RequestTraceSettings settings)
    {
        var defaults = new RequestTraceSettings();

        return settings with
        {
            MinimumLogLevel = string.IsNullOrWhiteSpace(settings.MinimumLogLevel) ? defaults.MinimumLogLevel : settings.MinimumLogLevel,
            MaximumBodyCharacters = settings.MaximumBodyCharacters <= 0 ? defaults.MaximumBodyCharacters : settings.MaximumBodyCharacters,
            CapturedContentTypePrefixes = settings.CapturedContentTypePrefixes.Length == 0 ? defaults.CapturedContentTypePrefixes : settings.CapturedContentTypePrefixes,
            ExcludedPathPrefixes = settings.ExcludedPathPrefixes.Length == 0 ? defaults.ExcludedPathPrefixes : settings.ExcludedPathPrefixes,
            RedactedHeaders = settings.RedactedHeaders.Length == 0 ? defaults.RedactedHeaders : settings.RedactedHeaders
        };
    }
}
