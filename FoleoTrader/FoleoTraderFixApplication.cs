using QuickFix;
using QuickFix.Fields;

namespace FoleoTrader;

public sealed class FoleoTraderFixApplication(FoleoTraderMessageMonitor monitor, ILogger<FoleoTraderFixApplication> logger) : MessageCracker, IApplication
{
    public void OnCreate(SessionID sessionID)
    {
    }

    public void OnLogon(SessionID sessionID)
    {
        logger.LogInformation("FoleoTrader accepted FIX logon for {SessionID}.", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        logger.LogInformation("FoleoTrader FIX logout for {SessionID}.", sessionID);
    }

    public void ToAdmin(Message message, SessionID sessionID) => monitor.Record("Sent", message, sessionID);

    public void FromAdmin(Message message, SessionID sessionID) => monitor.Record("Received", message, sessionID);

    public void ToApp(Message message, SessionID sessionID) => monitor.Record("Sent", message, sessionID);

    public void FromApp(Message message, SessionID sessionID)
    {
        monitor.Record("Received", message, sessionID);
        Crack(message, sessionID);
    }

    public void OnMessage(QuickFix.FIX50SP2.NewOrderSingle message, SessionID sessionID)
    {
        _ = SendRandomFillsAsync(message, sessionID);
    }

    private static async Task SendRandomFillsAsync(QuickFix.FIX50SP2.NewOrderSingle order, SessionID sessionID)
    {
        var clOrdID = order.GetString(Tags.ClOrdID);
        var requestedQuantity = order.GetDecimal(Tags.OrderQty);
        var quantity = decimal.Truncate(requestedQuantity);
        if (quantity <= 0m)
            return;

        var price = order.GetDecimal(Tags.Price);
        var side = order.GetChar(Tags.Side);
        var symbol = order.IsSetField(Tags.Symbol) ? order.GetString(Tags.Symbol) : string.Empty;
        var securityID = order.IsSetField(Tags.SecurityID) ? order.GetString(Tags.SecurityID) : string.Empty;
        var securityIDSource = order.IsSetField(Tags.SecurityIDSource) ? order.GetString(Tags.SecurityIDSource) : string.Empty;
        var currency = order.IsSetField(Tags.Currency) ? order.GetString(Tags.Currency) : string.Empty;
        var wholeQuantity = (int)Math.Min(quantity, int.MaxValue);
        var fillCount = Math.Min(Random.Shared.Next(1, 5), wholeQuantity);
        var remaining = wholeQuantity;
        var cumulative = 0m;
        var orderID = $"FOLEOTRADER-{Guid.CreateVersion7():N}";

        for (var index = 1; index <= fillCount; index++)
        {
            await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 6)));

            var lastQty = index == fillCount
                ? remaining
                : Random.Shared.Next(1, remaining - (fillCount - index) + 1);

            remaining -= lastQty;
            cumulative += lastQty;
            var lastPx = decimal.Round(price * (1m + Random.Shared.Next(-10, 11) / 10000m), 8);
            var leavesQty = Math.Max(0m, quantity - cumulative);
            var status = leavesQty == 0m ? OrdStatus.FILLED : OrdStatus.PARTIALLY_FILLED;
            var report = new QuickFix.FIX50SP2.ExecutionReport(
                new OrderID(orderID),
                new ExecID($"EXEC-{Guid.CreateVersion7():N}"),
                new ExecType(ExecType.TRADE),
                new OrdStatus(status),
                new Side(side),
                new LeavesQty(leavesQty),
                new CumQty(cumulative));

            report.Set(new ClOrdID(clOrdID));
            report.Set(new LastQty(lastQty));
            report.Set(new LastPx(lastPx));
            report.Set(new AvgPx(lastPx));
            report.Set(new OrderQty(quantity));
            report.Set(new Price(price));
            report.Set(new GrossTradeAmt(decimal.Round(lastQty * lastPx, 8)));

            if (!string.IsNullOrWhiteSpace(symbol))
                report.Set(new Symbol(symbol));
            if (!string.IsNullOrWhiteSpace(securityID))
                report.Set(new SecurityID(securityID));
            if (!string.IsNullOrWhiteSpace(securityIDSource))
                report.Set(new SecurityIDSource(securityIDSource));
            if (!string.IsNullOrWhiteSpace(currency))
                report.Set(new Currency(currency));

            Session.SendToTarget(report, sessionID);
        }
    }
}
