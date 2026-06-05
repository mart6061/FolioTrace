using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record TicketDetail : IModel
{
    public required TicketNumber TicketNumber { get; init; } = null!;
    public required TicketSide Side { get; init; }
    public required InstrumentID InstrumentID { get; init; } = null!;
    public required Alpha3 TradeCurrency { get; init; } = null!;
    public required TicketStage Stage { get; init; }
    public required TicketDecision ProposalDecision { get; init; }
    public required TicketDecision TradeDecision { get; init; }
    public required List<AccountID> AccountIDs { get; init; } = [];
    public Price? ProposalTargetPrice { get; init; }
    public TransactionQuantity? ProposalTotalAmount { get; init; }
    public required List<TicketProposalAllocation> ProposalAllocations { get; init; } = [];
    public required string ProposalReason { get; init; } = string.Empty;
    public required string ProposalAllocation { get; init; } = string.Empty;
    public Price? TradePrice { get; init; }
    public required List<TicketTradeAllocation> TradeAllocations { get; init; } = [];
    public required List<TicketFill> Fills { get; init; } = [];
    public required string TradeInstructionNotes { get; init; } = string.Empty;
    public required string TradeProgressNotes { get; init; } = string.Empty;
    public Instrument? Instrument { get; init; }
    public required List<Account> Accounts { get; init; } = [];
    public required EventDateTime ValuationDateTime { get; init; } = null!;
    public required AuditDateTime AsOfDateTime { get; init; } = null!;
    public required EventID LastEventID { get; init; } = null!;
    public required LastAuditDateTime LastAuditDateTime { get; init; } = null!;
    public bool IsActive => Stage is not TicketStage.Completed and not TicketStage.Cancelled;

    [JsonConstructor]
    [SetsRequiredMembers]
    public TicketDetail() { }

    public static TicketDetail From(Ticket ticket, Instrument? instrument, List<Account> accounts)
    {
        if (ticket is null)
            throw new ArgumentNullException(nameof(ticket));
        if (accounts is null)
            throw new ArgumentNullException(nameof(accounts));

        return new TicketDetail
        {
            TicketNumber = ticket.TicketNumber,
            Side = ticket.Side,
            InstrumentID = ticket.InstrumentID,
            TradeCurrency = ticket.TradeCurrency,
            Stage = ticket.Stage,
            ProposalDecision = ticket.ProposalDecision,
            TradeDecision = ticket.TradeDecision,
            AccountIDs = ticket.AccountIDs.ToList(),
            ProposalTargetPrice = ticket.ProposalTargetPrice,
            ProposalTotalAmount = ticket.ProposalTotalAmount,
            ProposalAllocations = ticket.ProposalAllocations.ToList(),
            ProposalReason = ticket.ProposalReason,
            ProposalAllocation = ticket.ProposalAllocation,
            TradePrice = ticket.TradePrice,
            TradeAllocations = ticket.TradeAllocations.ToList(),
            Fills = ticket.Fills.ToList(),
            TradeInstructionNotes = ticket.TradeInstructionNotes,
            TradeProgressNotes = ticket.TradeProgressNotes,
            Instrument = instrument,
            Accounts = accounts.ToList(),
            ValuationDateTime = ticket.ValuationDateTime,
            AsOfDateTime = ticket.AsOfDateTime,
            LastEventID = ticket.LastEventID,
            LastAuditDateTime = ticket.LastAuditDateTime
        };
    }
}
