namespace FoleoTrader.Models;

public sealed record FoleoTraderMessageEntry(
    DateTime TimestampUtc,
    string Direction,
    string SessionID,
    string MessageType,
    string Raw,
    IReadOnlyList<FoleoTraderMessageField> Fields);

public sealed record FoleoTraderMessageField(string Tag, string Name, string Value);
