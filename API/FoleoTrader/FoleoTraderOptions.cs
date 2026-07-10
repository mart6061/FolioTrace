namespace API.FoleoTrader;

public sealed class FoleoTraderOptions
{
    public const string SectionName = "FoleoTrader";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 9878;

    public string SenderCompID { get; init; } = "FOLEOAPI";

    public string TargetCompID { get; init; } = "FOLEOTRADER";

    public int HeartbeatSeconds { get; init; } = 20;

    public int IdleDisconnectMinutes { get; init; } = 5;

    public string BrokerLEI { get; init; } = "FOLEOTRADER000000001";

    public string StorePath { get; init; } = "FixStore/API";

    public string LogPath { get; init; } = "FixLog/API";
}
