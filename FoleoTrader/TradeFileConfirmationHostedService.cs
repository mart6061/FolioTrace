namespace FoleoTrader;

public sealed class TradeFileConfirmationHostedService(TradeFileSimulator simulator, ILogger<TradeFileConfirmationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            foreach (var file in simulator.Due())
                try { await simulator.ConfirmNextAsync(file, stoppingToken); }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(exception, "Unable to confirm ticket for TradeFile {TradeFileID}.", file.Request.TradeFileID);
                }
    }
}
