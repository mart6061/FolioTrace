namespace FoleoTrader;

public sealed class TradeFileConfirmationHostedService(TradeFileSimulator simulator, ILogger<TradeFileConfirmationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Selecting the due files is inside the guard as well as confirming them. Anything thrown out of
            // this loop reaches BackgroundServiceExceptionBehavior.StopHost and takes the whole simulator
            // down, so a single malformed delivery must not escape.
            try
            {
                foreach (var file in simulator.Due())
                    try { await simulator.ConfirmNextAsync(file, stoppingToken); }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        logger.LogWarning(exception, "Unable to confirm ticket for TradeFile {TradeFileID}.", file.Request.TradeFileID);
                    }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Unable to select due TradeFiles.");
            }
        }
    }
}
