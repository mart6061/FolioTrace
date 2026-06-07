using Microsoft.Extensions.Options;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace FoleoTrader;

public sealed class FoleoTraderFixAcceptorHostedService(
    IOptions<FoleoTraderOptions> options,
    FoleoTraderFixApplication application,
    ILogger<FoleoTraderFixAcceptorHostedService> logger) : IHostedService, IDisposable
{
    private readonly FoleoTraderOptions options = options.Value;
    private ThreadedSocketAcceptor? acceptor;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.StorePath);
        Directory.CreateDirectory(options.LogPath);

        var settings = new SessionSettings();
        var dictionary = new SettingsDictionary("FoleoTrader");
        dictionary.SetString("ConnectionType", "acceptor");
        dictionary.SetString("BeginString", "FIXT.1.1");
        dictionary.SetString("DefaultApplVerID", "FIX.5.0SP2");
        dictionary.SetString("SenderCompID", options.SenderCompID);
        dictionary.SetString("TargetCompID", options.TargetCompID);
        dictionary.SetString("SocketAcceptPort", options.Port.ToString());
        dictionary.SetString("HeartBtInt", options.HeartbeatSeconds.ToString());
        dictionary.SetString("StartTime", "00:00:00");
        dictionary.SetString("EndTime", "23:59:59");
        dictionary.SetString("UseDataDictionary", "N");
        dictionary.SetString("ResetOnLogon", "Y");
        dictionary.SetString("FileStorePath", options.StorePath);
        dictionary.SetString("FileLogPath", options.LogPath);

        settings.Set(new SessionID("FIXT.1.1", options.SenderCompID, options.TargetCompID), dictionary);
        acceptor = new ThreadedSocketAcceptor(application, new FileStoreFactory(settings), settings, new FileLogFactory(settings), new DefaultMessageFactory());
        acceptor.Start();
        logger.LogInformation("FoleoTrader FIX acceptor listening on port {Port}.", options.Port);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        acceptor?.Stop();
        return Task.CompletedTask;
    }

    public void Dispose() => acceptor?.Stop();
}
