using FolioTrace.Aggregates;

namespace API.FoleoTrader;

public sealed record FoleoTraderFixOrder(
    int TicketNumber,
    string ClOrdID,
    TicketSide Side,
    decimal Quantity,
    decimal Price,
    string Currency,
    string SecurityID,
    string SecurityIDSource,
    string Symbol);

public sealed record FoleoTraderExecutionReport(
    string ClOrdID,
    string ExecID,
    decimal LastQty,
    decimal LastPx,
    decimal CumQty,
    decimal LeavesQty,
    decimal GrossTradeAmt,
    string OrdStatus);
