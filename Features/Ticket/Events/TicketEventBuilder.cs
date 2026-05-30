using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class TicketEventBuilder
{
    public static TicketNumber NextTicketNumber(IEnumerable<ITicket> events)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        var max = events
            .OfType<TicketCreatedEvent>()
            .Select(@event => @event.TicketNumber.Value)
            .DefaultIfEmpty(0)
            .Max();
        return new TicketNumber(max + 1);
    }

    public static Result<TicketCreatedEvent> Create(TicketCreatedRequest request, TicketNumber ticketNumber, Instruments instruments) =>
        CreateResult(() =>
        {
            var messages = ValidateBase(request.UserID, request.EventDateTime, request.Reason);
            if (request.InstrumentID is null)
                messages.Add("InstrumentID is required.");
            if (!Enum.IsDefined(request.Side))
                messages.Add("Side is required.");
            if (instruments.Items.All(instrument => instrument.InstrumentID != request.InstrumentID || !instrument.Active))
                messages.Add($"InstrumentID '{request.InstrumentID}' does not exist or is inactive.");

            return messages.Count > 0
                ? Result<TicketCreatedEvent>.Invalid(messages)
                : Result<TicketCreatedEvent>.Success(new TicketCreatedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, ticketNumber, request.Side, request.InstrumentID!));
        });

    public static Result<TicketAccountAddedEvent> AddAccount(TicketAccountRequest request, Tickets tickets, Accounts accounts) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            ValidateAccount(request.AccountID, accounts, messages);
            if (ticket is not null && ticket.AccountIDs.Contains(request.AccountID))
                messages.Add($"AccountID '{request.AccountID}' is already on ticket '{request.TicketNumber}'.");

            return messages.Count > 0
                ? Result<TicketAccountAddedEvent>.Invalid(messages)
                : Result<TicketAccountAddedEvent>.Success(new TicketAccountAddedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.AccountID));
        });

    public static Result<TicketAccountRemovedEvent> RemoveAccount(TicketAccountRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (request.AccountID is null)
                messages.Add("AccountID is required.");
            if (ticket is not null && request.AccountID is not null && !ticket.AccountIDs.Contains(request.AccountID))
                messages.Add($"AccountID '{request.AccountID}' is not on ticket '{request.TicketNumber}'.");

            return messages.Count > 0
                ? Result<TicketAccountRemovedEvent>.Invalid(messages)
                : Result<TicketAccountRemovedEvent>.Success(new TicketAccountRemovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.AccountID!));
        });

    public static Result<TicketProposalCreatedEvent> CreateProposal(TicketProposalRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            ValidatePositiveDecimal(request.TargetPrice, "TargetPrice", messages);
            ValidatePositiveDecimal(request.TotalAmount, "TotalAmount", messages);
            ValidateProposalAllocations(request.Allocations, ticket, messages);

            return messages.Count > 0
                ? Result<TicketProposalCreatedEvent>.Invalid(messages)
                : Result<TicketProposalCreatedEvent>.Success(new TicketProposalCreatedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.TargetPrice, request.TotalAmount, request.Allocations));
        });

    public static Result<TicketProposalModifiedEvent> ModifyProposal(TicketProposalRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            ValidatePositiveDecimal(request.TargetPrice, "TargetPrice", messages);
            ValidatePositiveDecimal(request.TotalAmount, "TotalAmount", messages);
            ValidateProposalAllocations(request.Allocations, ticket, messages);

            return messages.Count > 0
                ? Result<TicketProposalModifiedEvent>.Invalid(messages)
                : Result<TicketProposalModifiedEvent>.Success(new TicketProposalModifiedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.TargetPrice, request.TotalAmount, request.Allocations));
        });

    public static Result<TicketProposalApprovedEvent> ApproveProposal(TicketApprovalRequest request, Tickets tickets) =>
        CreateDecisionEvent(request, tickets, TicketStatus.Proposal, () => new TicketProposalApprovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));

    public static Result<TicketProposalNotApprovedEvent> NotApproveProposal(TicketApprovalRequest request, Tickets tickets) =>
        CreateDecisionEvent(request, tickets, TicketStatus.Proposal, () => new TicketProposalNotApprovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));

    public static Result<TicketTradeCreatedEvent> CreateTrade(TicketTradeRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (ticket is not null && ticket.Status is not (TicketStatus.ProposalApproved or TicketStatus.Trade or TicketStatus.TradeNotApproved))
                messages.Add("Trade can only be created after proposal approval.");
            ValidatePositiveDecimal(request.TradedPrice, "TradedPrice", messages);
            ValidateTradeAllocations(request.Allocations, ticket, messages);

            return messages.Count > 0
                ? Result<TicketTradeCreatedEvent>.Invalid(messages)
                : Result<TicketTradeCreatedEvent>.Success(new TicketTradeCreatedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.TradedPrice, request.Allocations));
        });

    public static Result<TicketTradeModifiedEvent> ModifyTrade(TicketTradeRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (ticket is not null && ticket.Status is not (TicketStatus.ProposalApproved or TicketStatus.Trade or TicketStatus.TradeNotApproved))
                messages.Add("Trade can only be modified after proposal approval.");
            ValidatePositiveDecimal(request.TradedPrice, "TradedPrice", messages);
            ValidateTradeAllocations(request.Allocations, ticket, messages);

            return messages.Count > 0
                ? Result<TicketTradeModifiedEvent>.Invalid(messages)
                : Result<TicketTradeModifiedEvent>.Success(new TicketTradeModifiedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.TradedPrice, request.Allocations));
        });

    public static Result<TicketTradeFillAddedEvent> AddFill(TicketTradeFillRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (ticket is not null && ticket.Status is not (TicketStatus.Trade or TicketStatus.TradeNotApproved))
                messages.Add("Fills can only be changed while the ticket is in trade.");
            ValidatePositiveDecimal(request.Price, "Price", messages);
            ValidatePositiveDecimal(request.Quantity, "Quantity", messages);
            var fillID = request.FillID ?? Guid.NewGuid();
            if (fillID == Guid.Empty)
                messages.Add("FillID is required.");

            return messages.Count > 0
                ? Result<TicketTradeFillAddedEvent>.Invalid(messages)
                : Result<TicketTradeFillAddedEvent>.Success(new TicketTradeFillAddedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, fillID, request.Price, request.Quantity, request.Note ?? string.Empty));
        });

    public static Result<TicketTradeFillModifiedEvent> ModifyFill(TicketTradeFillRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (ticket is not null && ticket.Status is not (TicketStatus.Trade or TicketStatus.TradeNotApproved))
                messages.Add("Fills can only be changed while the ticket is in trade.");
            ValidatePositiveDecimal(request.Price, "Price", messages);
            ValidatePositiveDecimal(request.Quantity, "Quantity", messages);
            if (request.FillID is null || request.FillID == Guid.Empty)
                messages.Add("FillID is required.");
            if (ticket is not null && ticket.Fills.All(fill => fill.FillID != request.FillID))
                messages.Add($"FillID '{request.FillID}' is not on ticket '{request.TicketNumber}'.");

            return messages.Count > 0
                ? Result<TicketTradeFillModifiedEvent>.Invalid(messages)
                : Result<TicketTradeFillModifiedEvent>.Success(new TicketTradeFillModifiedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.FillID!.Value, request.Price, request.Quantity, request.Note ?? string.Empty));
        });

    public static Result<TicketTradeFillRemovedEvent> RemoveFill(TicketTradeFillRemovedRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
            if (request.FillID == Guid.Empty)
                messages.Add("FillID is required.");
            if (ticket is not null && ticket.Fills.All(fill => fill.FillID != request.FillID))
                messages.Add($"FillID '{request.FillID}' is not on ticket '{request.TicketNumber}'.");

            return messages.Count > 0
                ? Result<TicketTradeFillRemovedEvent>.Invalid(messages)
                : Result<TicketTradeFillRemovedEvent>.Success(new TicketTradeFillRemovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber, request.FillID));
        });

    public static Result<TicketTradeApprovedEvent> ApproveTrade(TicketApprovalRequest request, Tickets tickets) =>
        CreateDecisionEvent(request, tickets, TicketStatus.Trade, () => new TicketTradeApprovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));

    public static Result<TicketTradeNotApprovedEvent> NotApproveTrade(TicketApprovalRequest request, Tickets tickets) =>
        CreateDecisionEvent(request, tickets, TicketStatus.Trade, () => new TicketTradeNotApprovedEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));

    public static Result<TicketCancelledEvent> Cancel(TicketCancellationRequest request, Tickets tickets) =>
        CreateResult(() =>
        {
            var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var _);

            return messages.Count > 0
                ? Result<TicketCancelledEvent>.Invalid(messages)
                : Result<TicketCancelledEvent>.Success(new TicketCancelledEvent(NewEventID(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.TicketNumber));
        });

    private static Result<TEvent> CreateDecisionEvent<TEvent>(TicketApprovalRequest request, Tickets tickets, TicketStatus requiredStatus, Func<TEvent> create)
        where TEvent : class, ITicket
    {
        var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
        if (ticket is not null && ticket.Status != requiredStatus)
            messages.Add($"Ticket must be in {requiredStatus} status.");

        return messages.Count > 0
            ? Result<TEvent>.Invalid(messages)
            : Result<TEvent>.Success(create());
    }

    private static List<string> ValidateTicketMutation(UserID userID, EventDateTime eventDateTime, string reason, TicketNumber ticketNumber, Tickets tickets, out Ticket? ticket)
    {
        var messages = ValidateBase(userID, eventDateTime, reason);
        ticket = null;
        if (ticketNumber is null)
        {
            messages.Add("TicketNumber is required.");
            return messages;
        }

        ticket = tickets.Find(ticketNumber);
        if (ticket is null)
            messages.Add($"No matching ticket found for TicketNumber '{ticketNumber}'.");
        else if (ticket.Status == TicketStatus.Completed)
            messages.Add($"Ticket '{ticketNumber}' is complete and cannot be changed.");
        else if (ticket.Status == TicketStatus.Cancelled)
            messages.Add($"Ticket '{ticketNumber}' is cancelled and cannot be changed.");

        return messages;
    }

    private static void ValidateAccount(AccountID accountID, Accounts accounts, List<string> messages)
    {
        if (accountID is null)
        {
            messages.Add("AccountID is required.");
            return;
        }

        if (accounts.Items.All(account => account.AccountID != accountID || !account.Active))
            messages.Add($"AccountID '{accountID}' does not exist or is inactive.");
    }

    private static List<string> ValidateBase(UserID userID, EventDateTime eventDateTime, string reason)
    {
        var messages = new List<string>();
        if (userID is null) messages.Add("UserID is required.");
        if (eventDateTime is null) messages.Add("EventDateTime is required.");
        if (string.IsNullOrWhiteSpace(reason)) messages.Add("Reason is required.");
        return messages;
    }

    private static void ValidatePositiveDecimal(decimal value, string name, List<string> messages)
    {
        if (value <= 0)
            messages.Add($"{name} must be greater than zero.");
        if (decimal.Round(value, 8) != value)
            messages.Add($"{name} can have at most 8 decimal places.");
    }

    private static void ValidateProposalAllocations(IReadOnlyList<TicketProposalAllocation>? allocations, Ticket? ticket, List<string> messages)
    {
        if (ticket is not null && ticket.AccountIDs.Count == 0)
            messages.Add("Proposal requires at least one account on the ticket.");
        if (allocations is null || allocations.Count == 0)
        {
            messages.Add("At least one proposal allocation is required.");
            return;
        }

        ValidateAllocationAccounts(allocations.Select(allocation => allocation.AccountID), ticket, messages);
        foreach (var allocation in allocations)
            ValidatePositiveDecimal(allocation.Quantity, "Proposal allocation quantity", messages);
    }

    private static void ValidateTradeAllocations(IReadOnlyList<TicketTradeAllocation>? allocations, Ticket? ticket, List<string> messages)
    {
        if (allocations is null || allocations.Count == 0)
        {
            messages.Add("At least one trade allocation is required.");
            return;
        }

        ValidateAllocationAccounts(allocations.Select(allocation => allocation.AccountID), ticket, messages);
        foreach (var allocation in allocations)
        {
            ValidatePositiveDecimal(allocation.Quantity, "Trade allocation quantity", messages);
            ValidatePositiveDecimal(allocation.BookCost, "Trade allocation book cost", messages);
        }
    }

    private static void ValidateAllocationAccounts(IEnumerable<AccountID> allocationAccountIDs, Ticket? ticket, List<string> messages)
    {
        var accountIDs = allocationAccountIDs.ToList();
        if (accountIDs.Any(accountID => accountID is null))
            messages.Add("Allocation AccountID is required.");
        var populatedAccountIDs = accountIDs.Where(accountID => accountID is not null).ToList();
        if (populatedAccountIDs.Select(accountID => accountID.Value).Distinct().Count() != populatedAccountIDs.Count)
            messages.Add("Allocations must contain each AccountID only once.");
        if (ticket is not null)
        {
            foreach (var accountID in populatedAccountIDs.Where(accountID => !ticket.AccountIDs.Contains(accountID)))
                messages.Add($"Allocation AccountID '{accountID}' is not on ticket '{ticket.TicketNumber}'.");
        }
    }

    private static Result<T> CreateResult<T>(Func<Result<T>> create) => create();

    private static EventID NewEventID() => new(Guid.NewGuid());
}
