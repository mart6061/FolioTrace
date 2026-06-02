using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    private static Result<TEvent> CreateProposalDecisionEvent<TEvent>(TicketApprovalRequest request, Tickets tickets, Func<TEvent> create)
        where TEvent : class, ITicket
    {
        var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
        if (ticket is not null)
        {
            if (ticket.Stage != TicketStage.Proposal)
                messages.Add("Ticket must be in Proposal stage.");
            if (ticket.ProposalDecision != TicketDecision.PendingApproval)
                messages.Add("Proposal decision must be pending approval.");
            if (ticket.ProposalAllocations.Count == 0)
                messages.Add("Proposal allocations are required before proposal approval.");
        }

        return messages.Count > 0
            ? Result<TEvent>.Invalid(messages)
            : Result<TEvent>.Success(create());
    }

    private static Result<TEvent> CreateProposalTextSetEvent<TEvent>(TicketTextSetRequest request, Tickets tickets, Func<TEvent> create)
        where TEvent : class, ITicket
    {
        var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
        if (ticket is not null && ticket.Stage != TicketStage.Proposal)
            messages.Add("Ticket must be in Proposal stage.");

        return messages.Count > 0
            ? Result<TEvent>.Invalid(messages)
            : Result<TEvent>.Success(create());
    }

    private static Result<TEvent> CreateTradeDecisionEvent<TEvent>(TicketApprovalRequest request, Tickets tickets, Func<TEvent> create)
        where TEvent : class, ITicket
    {
        var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
        if (ticket is not null)
        {
            if (ticket.Stage != TicketStage.Trade)
                messages.Add("Ticket must be in Trade stage.");
            if (ticket.TradeDecision != TicketDecision.PendingApproval)
                messages.Add("Trade decision must be pending.");
            if (ticket.TradeAllocations.Count == 0)
                messages.Add("Trade allocations are required before trade approval.");
        }

        return messages.Count > 0
            ? Result<TEvent>.Invalid(messages)
            : Result<TEvent>.Success(create());
    }

    private static Result<TEvent> CreateTradeTextSetEvent<TEvent>(TicketTextSetRequest request, Tickets tickets, Func<TEvent> create)
        where TEvent : class, ITicket
    {
        var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out var ticket);
        if (ticket is not null && ticket.Stage != TicketStage.Trade)
            messages.Add("Ticket must be in Trade stage.");

        return messages.Count > 0
            ? Result<TEvent>.Invalid(messages)
            : Result<TEvent>.Success(create());
    }

    private static void ValidateTradeEntry(Ticket? ticket, List<string> messages, string verb)
    {
        if (ticket is null)
            return;
        if (ticket.Stage != TicketStage.Trade || ticket.ProposalDecision != TicketDecision.Approved)
            messages.Add($"Trade can only be {verb} after proposal approval.");
    }

    private static void ValidateProposalEntry(Ticket? ticket, List<string> messages, string verb)
    {
        if (ticket is null)
            return;
        if (ticket.Stage != TicketStage.Proposal)
            messages.Add($"Proposal can only be {verb} while the ticket is in Proposal stage.");
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
        else if (ticket.Stage == TicketStage.Completed)
            messages.Add($"Ticket '{ticketNumber}' is complete and cannot be changed.");
        else if (ticket.Stage == TicketStage.Cancelled)
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

    private static void ValidatePrice(Price? value, string name, List<string> messages)
    {
        if (value is null)
        {
            messages.Add($"{name} is required.");
            return;
        }

        ValidatePositiveDecimal(value.Amount, name, messages);
    }

    private static void ValidateTransactionQuantity(TransactionQuantity? value, string name, List<string> messages)
    {
        if (value is null)
        {
            messages.Add($"{name} is required.");
            return;
        }

        ValidatePositiveDecimal(value.Value, name, messages);
    }

    private static Alpha3? ResolveTradeCurrency(TicketProposalRequest request, Ticket? ticket, List<string> messages)
    {
        var tradeCurrency = request.TradeCurrency ?? ticket?.TradeCurrency;
        if (tradeCurrency is null)
            messages.Add("TradeCurrency is required.");
        return tradeCurrency;
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
