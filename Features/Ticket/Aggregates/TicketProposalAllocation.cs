using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketProposalAllocation(AccountID AccountID, decimal Quantity);
