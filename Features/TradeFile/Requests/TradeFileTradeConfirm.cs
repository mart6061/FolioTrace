namespace FolioTrace.Aggregates;
public sealed record TradeFileTradeConfirm(Guid ConfirmationID, Guid TradeFileID, int TicketNumber, decimal Quantity, decimal Price, DateTime ConfirmedAtUtc);
