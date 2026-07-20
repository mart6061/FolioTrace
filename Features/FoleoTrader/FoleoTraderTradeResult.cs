namespace FolioTrace.Aggregates;

public sealed record FoleoTraderTradeResult(ITicket? TradeEvent, IReadOnlyList<string> ValidationErrors);
