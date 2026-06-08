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
    private TaskCompletionSource<SessionID>? logonCompletion;
    private bool loggedOn;
    private DateTime lastActivityUtc = DateTime.MinValue;
    private Timer? idleTimer;

    public FoleoTraderFixClient(IOptions<FoleoTraderOptions> options, FoleoTraderOrderProcessor processor, ILogger<FoleoTraderFixClient> logger)
    {
        this.options = options.Value;
        this.processor = processor;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        idleTimer = new Timer(_ => StopIfIdle(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        await ReplayStoredExecutionReportsAsync(cancellationToken);
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

    public async Task SendAsync(FoleoTraderFixOrder order, CancellationToken cancellationToken)
    {
        EnsureStarted();

        var session = await WaitForLogonAsync(cancellationToken);
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

        if (!Session.SendToTarget(message, session))
            throw new InvalidOperationException("FoleoTrader FIX order was not sent because the session is not logged on.");

        MarkActivity();
        Session.SendToTarget(message, session);
        MarkActivity();

        return Task.CompletedTask;
    }

    public void OnCreate(SessionID sessionID)
    {
        lock (syncRoot)
        {
            this.sessionID = sessionID;
        }
        this.sessionID = sessionID;
    }

    public void OnLogon(SessionID sessionID)
    {
        lock (syncRoot)
        {
            this.sessionID = sessionID;
            loggedOn = true;
            logonCompletion?.TrySetResult(sessionID);
            MarkActivity();
        }

        this.sessionID = sessionID;
        MarkActivity();
        logger.LogInformation("Logged on to FoleoTrader FIX session {SessionID}.", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        lock (syncRoot)
        {
            loggedOn = false;
            if (logonCompletion is null || logonCompletion.Task.IsCompleted)
                logonCompletion = CreateLogonCompletion();
        }

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
        var report = CreateExecutionReport(message);
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
            settings.Get().SetString("FileStorePath", options.StorePath);
            settings.Get().SetString("FileLogPath", options.LogPath);
            logonCompletion = CreateLogonCompletion();
            loggedOn = false;

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

    private async Task<SessionID> WaitForLogonAsync(CancellationToken cancellationToken)
    {
        Task<SessionID> logonTask;
        SessionID? session;

        lock (syncRoot)
        {
            session = sessionID;
            if (loggedOn && session is not null)
                return session;

            logonTask = logonCompletion?.Task ?? throw new InvalidOperationException("FoleoTrader FIX session has not been created.");
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, options.HeartbeatSeconds)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            return await logonTask.WaitAsync(linked.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Timed out waiting for the FoleoTrader FIX session to log on.");
        }
    }

    private async Task ReplayStoredExecutionReportsAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(options.LogPath))
            return;

        foreach (var path in Directory.EnumerateFiles(options.LogPath, "*.messages.current.log"))
        {
            foreach (var line in File.ReadLines(path))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!line.Contains("35=8", StringComparison.Ordinal))
                    continue;

                var messageStart = line.IndexOf(" : ", StringComparison.Ordinal);
                if (messageStart < 0)
                    continue;

                try
                {
                    var message = new Message(line[(messageStart + 3)..]);
                    if (message.Header.GetString(Tags.MsgType) != MsgType.EXECUTION_REPORT)
                        continue;

                    await processor.ProcessExecutionReportAsync(CreateExecutionReport(message), cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Skipped stored FoleoTrader execution report from {Path}.", path);
                }
            }
        }
    }

    private static FoleoTraderExecutionReport CreateExecutionReport(Message message) =>
        new(
            message.GetString(Tags.ClOrdID),
            message.GetString(Tags.ExecID),
            message.IsSetField(Tags.LastQty) ? message.GetDecimal(Tags.LastQty) : 0m,
            message.IsSetField(Tags.LastPx) ? message.GetDecimal(Tags.LastPx) : 0m,
            message.GetDecimal(Tags.CumQty),
            message.GetDecimal(Tags.LeavesQty),
            message.IsSetField(Tags.GrossTradeAmt) ? message.GetDecimal(Tags.GrossTradeAmt) : 0m,
            message.GetChar(Tags.OrdStatus).ToString());

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
            loggedOn = false;
            logonCompletion?.TrySetCanceled();
            logonCompletion = null;
        }
    }

    private void MarkActivity() => lastActivityUtc = DateTime.UtcNow;

    private static TaskCompletionSource<SessionID> CreateLogonCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
