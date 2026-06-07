using Microsoft.Extensions.Options;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace API.FoleoTrader;

public sealed class FoleoTraderFixClient : MessageCracker, IApplication, IHostedService, IDisposable
{
    private readonly FoleoTraderOptions options;
    private readonly FoleoTraderOrderProcessor processor;
    private readonly ILogger<FoleoTraderFixClient> logger;
    private readonly object syncRoot = new();
    private SocketInitiator? initiator;
    private SessionID? sessionID;
    private DateTime lastActivityUtc = DateTime.MinValue;
    private Timer? idleTimer;

    public FoleoTraderFixClient(IOptions<FoleoTraderOptions> options, FoleoTraderOrderProcessor processor, ILogger<FoleoTraderFixClient> logger)
    {
        this.options = options.Value;
        this.processor = processor;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        idleTimer = new Timer(_ => StopIfIdle(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopInitiator();
        idleTimer?.Dispose();
        return Task.CompletedTask;
    }

    public Task SendAsync(FoleoTraderFixOrder order, CancellationToken cancellationToken)
    {
        EnsureStarted();

        var session = sessionID ?? throw new InvalidOperationException("FoleoTrader FIX session has not been created.");
        var message = new QuickFix.FIX50SP2.NewOrderSingle(
            new ClOrdID(order.ClOrdID),
            new Side(order.Side == FolioTrace.Aggregates.TicketSide.Buy ? Side.BUY : Side.SELL),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT));

        message.Set(new OrderQty(order.Quantity));
        message.Set(new Price(order.Price));
        message.Set(new Currency(order.Currency));
        message.Set(new Symbol(order.Symbol));
        message.Set(new SecurityID(order.SecurityID));
        message.Set(new SecurityIDSource(order.SecurityIDSource));

        Session.SendToTarget(message, session);
        MarkActivity();

        return Task.CompletedTask;
    }

    public void OnCreate(SessionID sessionID)
    {
        this.sessionID = sessionID;
    }

    public void OnLogon(SessionID sessionID)
    {
        this.sessionID = sessionID;
        MarkActivity();
        logger.LogInformation("Logged on to FoleoTrader FIX session {SessionID}.", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        logger.LogInformation("Logged out from FoleoTrader FIX session {SessionID}.", sessionID);
    }

    public void ToAdmin(Message message, SessionID sessionID) => MarkActivity();

    public void FromAdmin(Message message, SessionID sessionID) => MarkActivity();

    public void ToApp(Message message, SessionID sessionID) => MarkActivity();

    public void FromApp(Message message, SessionID sessionID)
    {
        MarkActivity();
        Crack(message, sessionID);
    }

    public void OnMessage(QuickFix.FIX50SP2.ExecutionReport message, SessionID sessionID)
    {
        var report = new FoleoTraderExecutionReport(
            message.GetString(Tags.ClOrdID),
            message.GetString(Tags.ExecID),
            message.IsSetField(Tags.LastQty) ? message.GetDecimal(Tags.LastQty) : 0m,
            message.IsSetField(Tags.LastPx) ? message.GetDecimal(Tags.LastPx) : 0m,
            message.GetDecimal(Tags.CumQty),
            message.GetDecimal(Tags.LeavesQty),
            message.IsSetField(Tags.GrossTradeAmt) ? message.GetDecimal(Tags.GrossTradeAmt) : 0m,
            message.GetChar(Tags.OrdStatus).ToString());

        _ = processor.ProcessExecutionReportAsync(report, CancellationToken.None);
    }

    public void Dispose()
    {
        StopInitiator();
        idleTimer?.Dispose();
    }

    private void EnsureStarted()
    {
        lock (syncRoot)
        {
            if (initiator is not null)
            {
                MarkActivity();
                return;
            }

            Directory.CreateDirectory(options.StorePath);
            Directory.CreateDirectory(options.LogPath);

            var settings = new SessionSettings();
            var dictionary = new SettingsDictionary("FoleoTrader");
            dictionary.SetString("ConnectionType", "initiator");
            dictionary.SetString("BeginString", "FIXT.1.1");
            dictionary.SetString("DefaultApplVerID", "FIX.5.0SP2");
            dictionary.SetString("SenderCompID", options.SenderCompID);
            dictionary.SetString("TargetCompID", options.TargetCompID);
            dictionary.SetString("SocketConnectHost", options.Host);
            dictionary.SetString("SocketConnectPort", options.Port.ToString());
            dictionary.SetString("HeartBtInt", options.HeartbeatSeconds.ToString());
            dictionary.SetString("StartTime", "00:00:00");
            dictionary.SetString("EndTime", "23:59:59");
            dictionary.SetString("UseDataDictionary", "N");
            dictionary.SetString("ResetOnLogon", "Y");
            dictionary.SetString("FileStorePath", options.StorePath);
            dictionary.SetString("FileLogPath", options.LogPath);

            var session = new SessionID("FIXT.1.1", options.SenderCompID, options.TargetCompID);
            settings.Set(session, dictionary);

            initiator = new SocketInitiator(this, new FileStoreFactory(settings), settings, new FileLogFactory(settings), new DefaultMessageFactory());
            initiator.Start();
            sessionID = session;
            MarkActivity();
        }
    }

    private void StopIfIdle()
    {
        lock (syncRoot)
        {
            if (initiator is null)
                return;

            if (DateTime.UtcNow - lastActivityUtc < TimeSpan.FromMinutes(options.IdleDisconnectMinutes))
                return;

            StopInitiator();
        }
    }

    private void StopInitiator()
    {
        lock (syncRoot)
        {
            initiator?.Stop();
            initiator = null;
            sessionID = null;
        }
    }

    private void MarkActivity() => lastActivityUtc = DateTime.UtcNow;
}
