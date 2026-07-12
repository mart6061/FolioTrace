namespace API.TradeFiles;

public sealed class TradeFileOptions
{
    public const string SectionName = "TradeFiles";
    public string FoleoTraderReceiverUrl { get; set; } = "https://localhost:5001/trade-files";
    public string ApiCallbackBaseUrl { get; set; } = "https://localhost:7058/API/TradeFiles";
    public int ProcessingIntervalSeconds { get; set; } = 2;
}
