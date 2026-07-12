namespace API.TradeFiles;

public sealed class TradeFileProcessingHostedService(
    TradeFileWorkflowService workflow,
    Microsoft.Extensions.Options.IOptions<TradeFileOptions> options,
    ApiReadinessState readinessState,
    ILogger<TradeFileProcessingHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await readinessState.WaitUntilReadyAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, options.Value.ProcessingIntervalSeconds)));
        do
        {
            try { await workflow.ProcessPendingAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Trade file processing cycle failed."); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
