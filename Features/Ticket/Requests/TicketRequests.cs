using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketSide Side,
    InstrumentID InstrumentID);

public sealed record TicketAccountRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    AccountID AccountID);

public sealed record TicketProposalRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    decimal TargetPrice,
    decimal TotalAmount,
    IReadOnlyList<TicketProposalAllocation> Allocations);

public sealed record TicketApprovalRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber);

public sealed record TicketTradeRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    decimal TradedPrice,
    IReadOnlyList<TicketTradeAllocation> Allocations);

public sealed record TicketTradeFillRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    Guid? FillID,
    decimal Price,
    decimal Quantity,
    string? Note);

public sealed record TicketTradeFillRemovedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber,
    Guid FillID);

public sealed record TicketCancellationRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    TicketNumber TicketNumber);
