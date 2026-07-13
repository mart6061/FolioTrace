namespace FolioTrace.Aggregates;

public sealed record FIXTradeMethod(string Host, int Port, string SenderCompID, string TargetCompID, int HeartbeatSeconds = 20) : ITradeMethod
{
    public TradeMethodType Type => TradeMethodType.FIX;
}
