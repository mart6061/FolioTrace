using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TicketCreatedEvent), nameof(TicketCreatedEvent))]
[JsonDerivedType(typeof(TicketAccountAddedEvent), nameof(TicketAccountAddedEvent))]
[JsonDerivedType(typeof(TicketAccountRemovedEvent), nameof(TicketAccountRemovedEvent))]
[JsonDerivedType(typeof(TicketProposalCreatedEvent), nameof(TicketProposalCreatedEvent))]
[JsonDerivedType(typeof(TicketProposalModifiedEvent), nameof(TicketProposalModifiedEvent))]
[JsonDerivedType(typeof(TicketProposalDecisionRequestedEvent), nameof(TicketProposalDecisionRequestedEvent))]
[JsonDerivedType(typeof(TicketProposalApprovedEvent), nameof(TicketProposalApprovedEvent))]
[JsonDerivedType(typeof(TicketProposalNotApprovedEvent), nameof(TicketProposalNotApprovedEvent))]
[JsonDerivedType(typeof(TicketProposalReasonSetEvent), nameof(TicketProposalReasonSetEvent))]
[JsonDerivedType(typeof(TicketProposalAllocationSetEvent), nameof(TicketProposalAllocationSetEvent))]
[JsonDerivedType(typeof(TicketTradeCreatedEvent), nameof(TicketTradeCreatedEvent))]
[JsonDerivedType(typeof(TicketTradeModifiedEvent), nameof(TicketTradeModifiedEvent))]
[JsonDerivedType(typeof(TicketTradeFillAddedEvent), nameof(TicketTradeFillAddedEvent))]
[JsonDerivedType(typeof(TicketTradeFillModifiedEvent), nameof(TicketTradeFillModifiedEvent))]
[JsonDerivedType(typeof(TicketTradeFillRemovedEvent), nameof(TicketTradeFillRemovedEvent))]
[JsonDerivedType(typeof(TicketTradeDecisionRequestedEvent), nameof(TicketTradeDecisionRequestedEvent))]
[JsonDerivedType(typeof(TicketTradeApprovedEvent), nameof(TicketTradeApprovedEvent))]
[JsonDerivedType(typeof(TicketTradeNotApprovedEvent), nameof(TicketTradeNotApprovedEvent))]
[JsonDerivedType(typeof(TicketTradeInstructionNotesSetEvent), nameof(TicketTradeInstructionNotesSetEvent))]
[JsonDerivedType(typeof(TicketTradeProgressNotesSetEvent), nameof(TicketTradeProgressNotesSetEvent))]
[JsonDerivedType(typeof(TicketCancelledEvent), nameof(TicketCancelledEvent))]
public interface ITicket : IEventBase
{
    TicketNumber TicketNumber { get; }
}
