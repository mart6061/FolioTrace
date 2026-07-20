using Microsoft.Extensions.Options;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using FolioTrace.Aggregates;

namespace API.FoleoTrader;

public sealed class FoleoTraderFixClient : MessageCracker, IApplication, IHostedService, IDisposable
{
    private readonly FoleoTraderConnectionOptions options;
    private readonly FoleoTraderOrderProcessor processor;
    private readonly FoleoTraderFIXOperationRecorder operationRecorder;
    private readonly ApiReadinessState readinessState;
    private readonly FixStartupHealthState startupHealthState;
    private readonly ILogger<FoleoTraderFixClient> logger;
    private readonly object syncRoot = new();
    private SocketInitiator? initiator;
    private SessionID? sessionID;
    private TaskCompletionSource<SessionID>? logonCompletion;
    private bool loggedOn;
    private DateTime lastActivityUtc = DateTime.MinValue;
    private Timer? idleTimer;
    private FIXTradeMethod? activeMethod;

    public FoleoTraderFixClient(IOptions<FoleoTraderConnectionOptions> options, FoleoTraderOrderProcessor processor, FoleoTraderFIXOperationRecorder operationRecorder, ApiReadinessState readinessState, FixStartupHealthState startupHealthState, ILogger<FoleoTraderFixClient> logger)
    {
        this.options = options.Value;
        this.processor = processor;
        this.operationRecorder = operationRecorder;
        this.readinessState = readinessState;
        this.startupHealthState = startupHealthState;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = StartWhenReadyAsync(cancellationToken);
        return Task.CompletedTask;
    }

    private async Task StartWhenReadyAsync(CancellationToken cancellationToken)
    {
        await RunStartupAsync(async token =>
        {
            await readinessState.WaitUntilReadyAsync(token);
            idleTimer = new Timer(_ => StopIfIdle(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            await ReplayStoredExecutionReportsAsync(token);
        }, startupHealthState, logger, cancellationToken);
    }

    internal static async Task RunStartupAsync(
        Func<CancellationToken, Task> startup,
        FixStartupHealthState healthState,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await startup(cancellationToken);
            healthState.MarkReady();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal hosted-service shutdown.
        }
        catch (Exception exception)
        {
            healthState.MarkFailed(exception);
            logger.LogCritical(exception, "FoleoTrader FIX startup or stored execution-report replay failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopInitiator();
        idleTimer?.Dispose();
        return Task.CompletedTask;
    }

    public async Task SendAsync(FoleoTraderFixOrder order, FIXTradeMethod method, CancellationToken cancellationToken)
    {
        EnsureStarted(method);

        var session = await WaitForLogonAsync(cancellationToken);
        var message = new QuickFix.FIX50SP2.NewOrderSingle(
            new ClOrdID(order.ClOrdID),
            new Side(order.Side == FolioTrace.Aggregates.TicketSide.Buy ? Side.BUY : Side.SELL),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT));

        message.Set(new OrderQty(order.Quantity));
        message.Set(new Price(order.Price));
        message.Set(new QuickFix.Fields.Currency(order.Currency));
        message.Set(new Symbol(order.Symbol));
        message.Set(new SecurityID(order.SecurityID));
        message.Set(new SecurityIDSource(order.SecurityIDSource));

        if (!Session.SendToTarget(message, session))
            throw new InvalidOperationException("FoleoTrader FIX order was not sent because the session is not logged on.");

        MarkActivity();
    }

    public void OnCreate(SessionID sessionID)
    {
        lock (syncRoot)
        {
            this.sessionID = sessionID;
        }
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

    public void ToAdmin(Message message, SessionID sessionID)
    {
        MarkActivity();
        RecordFIXOperation("Outbound", "Admin", message, sessionID);
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
        MarkActivity();
        RecordFIXOperation("Inbound", "Admin", message, sessionID);
    }

    public void ToApp(Message message, SessionID sessionID)
    {
        MarkActivity();
        RecordFIXOperation("Outbound", "App", message, sessionID);
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        MarkActivity();
        RecordFIXOperation("Inbound", "App", message, sessionID);
        Crack(message, sessionID);
    }

    public void OnMessage(QuickFix.FIX50SP2.ExecutionReport message, SessionID sessionID)
    {
        var report = CreateExecutionReport(message);
        _ = processor.ProcessExecutionReportAsync(report, CancellationToken.None);
    }

    public void Dispose()
    {
        StopInitiator();
        idleTimer?.Dispose();
    }

    private void EnsureStarted(FIXTradeMethod method)
    {
        lock (syncRoot)
        {
            if (initiator is not null && activeMethod == method)
            {
                MarkActivity();
                return;
            }
            if (initiator is not null)
                StopInitiator();

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
            dictionary.SetString("SenderCompID", method.SenderCompID);
            dictionary.SetString("TargetCompID", method.TargetCompID);
            dictionary.SetString("SocketConnectHost", method.Host);
            dictionary.SetString("SocketConnectPort", method.Port.ToString());
            dictionary.SetString("HeartBtInt", method.HeartbeatSeconds.ToString());
            dictionary.SetString("StartTime", "00:00:00");
            dictionary.SetString("EndTime", "23:59:59");
            dictionary.SetString("UseDataDictionary", "N");
            dictionary.SetString("ResetOnLogon", "Y");
            dictionary.SetString("FileStorePath", options.StorePath);
            dictionary.SetString("FileLogPath", options.LogPath);

            var session = new SessionID("FIXT.1.1", method.SenderCompID, method.TargetCompID);
            settings.Set(session, dictionary);

            initiator = new SocketInitiator(this, new FileStoreFactory(settings), settings, new FileLogFactory(settings), new DefaultMessageFactory());
            initiator.Start();
            activeMethod = method;
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
            activeMethod = null;
            sessionID = null;
            loggedOn = false;
            logonCompletion?.TrySetCanceled();
            logonCompletion = null;
        }
    }

    private void MarkActivity() => lastActivityUtc = DateTime.UtcNow;

    private void RecordFIXOperation(string direction, string channel, Message message, SessionID sessionID)
    {
        Task recordTask;

        try
        {
            recordTask = operationRecorder.RecordAsync(direction, channel, message, sessionID, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to record FoleoTrader FIX {Direction} {Channel} message.", direction, channel);
            return;
        }

        if (recordTask.IsCompletedSuccessfully)
            return;

        _ = recordTask.ContinueWith(
            task => logger.LogWarning(task.Exception, "Failed to record FoleoTrader FIX {Direction} {Channel} message.", direction, channel),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    private static TaskCompletionSource<SessionID> CreateLogonCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
