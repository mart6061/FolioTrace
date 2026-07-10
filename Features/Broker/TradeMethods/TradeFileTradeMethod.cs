namespace FolioTrace.Aggregates;

public sealed record TradeFileTradeMethod(FileNameTemplate FileNameTemplate, List<TradeFileColumn> Columns, ITradeMethodFileSendConfig SendConfig) : ITradeMethod
{
    public TradeMethodType Type => TradeMethodType.TradeFile;
}
