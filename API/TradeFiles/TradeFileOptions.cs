namespace API.TradeFiles;

public sealed class TradeFileOptions
{
    public const string SectionName = "TradeFiles";
    public string FoleoTraderReceiverUrl { get; set; } = "https://localhost:5001/trade-files";
    public string ApiCallbackBaseUrl { get; set; } = "https://localhost:7058/API/TradeFiles";
    public int ProcessingIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// Shared secret required on TradeFile callback requests (Acknowledgements/Confirmations).
    /// When set, callers must present it in the <c>X-FolioTrace-Callback-Secret</c> header.
    /// Leave empty to disable enforcement (not recommended outside local development).
    /// </summary>
    public string CallbackSecret { get; set; } = string.Empty;

    public const string CallbackSecretHeaderName = "X-FolioTrace-Callback-Secret";
}
