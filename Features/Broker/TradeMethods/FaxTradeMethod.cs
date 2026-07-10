namespace FolioTrace.Aggregates;

public sealed record FaxTradeMethod(TelephoneNumber TelephoneNumber) : ITradeMethod
{
    public TradeMethodType Type => TradeMethodType.Fax;
}
