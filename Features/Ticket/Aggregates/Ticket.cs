using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Ticket : IModel
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
    public required EventDateTime ValuationDateTime { get; init; } = null!;
    public required AuditDateTime AsOfDateTime { get; init; } = null!;
    public required EventID LastEventID { get; init; } = null!;
    public required LastAuditDateTime LastAuditDateTime { get; init; } = null!;
    public bool IsActive => Stage is not TicketStage.Completed and not TicketStage.Cancelled;

    [JsonConstructor]
    [SetsRequiredMembers]
    public Ticket() { }

    public string ToData() => $"{TicketNumber.ToData()}|{Side}|{InstrumentID.ToData()}|{TradeCurrency.ToData()}|{Stage}|{ProposalDecision}|{TradeDecision}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(Ticket)}: ({TicketNumber.ToDetail()}, Side: {Side}, InstrumentID: {InstrumentID.ToDetail()}, TradeCurrency: {TradeCurrency.ToDetail()}, Stage: {Stage}, ProposalDecision: {ProposalDecision}, TradeDecision: {TradeDecision})";
}
