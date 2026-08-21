using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketTradeApprovalTransactionResult(
    TicketTradeApprovedEvent ApprovalEvent,
    IReadOnlyList<HoldingCreatedEvent> HoldingEvents,
    IReadOnlyList<ITransactionMovementEvent> TransactionEvents);
