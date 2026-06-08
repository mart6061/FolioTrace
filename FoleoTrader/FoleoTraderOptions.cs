namespace FoleoTrader;

public sealed class FoleoTraderOptions
{
    public const string SectionName = "FoleoTrader";

    public int Port { get; init; } = 9878;

    public string SenderCompID { get; init; } = "FOLEOTRADER";

    public string TargetCompID { get; init; } = "FOLEOAPI";

    public int HeartbeatSeconds { get; init; } = 20;

    public string StorePath { get; init; } = "FixStore/FoleoTrader";

    public string LogPath { get; init; } = "FixLog/FoleoTrader";

    public string MonitorPath { get; init; } = "FixLog/FoleoTrader/messages.jsonl";
}
