namespace FolioTrace.Aggregates;

public sealed record PhoneTradeMethod(TelephoneNumber TelephoneNumber) : ITradeMethod
{
    public TradeMethodType Type => TradeMethodType.Phone;
}
