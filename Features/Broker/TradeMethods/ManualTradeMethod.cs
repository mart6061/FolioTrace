namespace FolioTrace.Aggregates;

public sealed record ManualTradeMethod(bool Enabled = true) : ITradeMethod
{
    public TradeMethodType Type => TradeMethodType.Manual;
}
