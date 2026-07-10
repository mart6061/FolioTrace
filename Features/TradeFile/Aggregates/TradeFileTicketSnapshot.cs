using FolioTrace.Types;
namespace FolioTrace.Aggregates;
public sealed record TradeFileTicketSnapshot(TicketNumber TicketNumber, string ISIN, string Sedol, decimal Quantity, Price Price, Alpha3 Currency);
