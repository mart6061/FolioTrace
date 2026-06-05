using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class TicketBuilderTests
{
    [Fact]
    public void TicketNumber_RejectsInvalidIntegers()
    {
        var exception = Assert.Throws<ArgumentException>(() => new TicketNumber(0));

        Assert.Contains("greater than zero", exception.Message);
    }

    [Fact]
    public void Create_AssignsSequentialTicketNumbers()
    {
        var first = TicketEventBuilder.Create(CreateRequest(TicketSide.Buy), new TicketNumber(1), CreateInstruments()).Value!;
        var secondNumber = TicketEventBuilder.NextTicketNumber([first]);

        var second = TicketEventBuilder.Create(CreateRequest(TicketSide.Sell), secondNumber, CreateInstruments()).Value!;

        Assert.Equal(1, first.TicketNumber.Value);
        Assert.Equal(2, second.TicketNumber.Value);
    }

    [Fact]
    public void CreatedTicket_StartsAtProposal()
    {
        var created = CreateTicket();

        var ticket = Assert.Single(new Tickets(EventDate, [created]).Items);

        Assert.Equal(TicketStage.Proposal, ticket.Stage);
        Assert.Equal(TicketDecision.InProgress, ticket.ProposalDecision);
        Assert.Equal(TicketDecision.InProgress, ticket.TradeDecision);
        Assert.Equal(string.Empty, ticket.ProposalReason);
        Assert.Equal(string.Empty, ticket.ProposalAllocation);
        Assert.Equal(string.Empty, ticket.TradeInstructionNotes);
        Assert.Equal(string.Empty, ticket.TradeProgressNotes);
    }

    [Fact]
    public void AccountAddRemove_UpdatesSingleTicketAggregate()
    {
        var created = CreateTicket();
        var tickets = new Tickets(EventDate, [created]);
        var added = TicketEventBuilder.AddAccount(new TicketAccountRequest(UserID, EventDate, "Add account", created.TicketNumber, AccountID), tickets, CreateAccounts()).Value!;
        tickets = new Tickets(EventDate, [created, added]);
        var removed = TicketEventBuilder.RemoveAccount(new TicketAccountRequest(UserID, EventDate, "Remove account", created.TicketNumber, AccountID), tickets).Value!;

        var afterAdd = Assert.Single(tickets.Items);
        Assert.Contains(AccountID, afterAdd.AccountIDs);

        var afterRemove = new Tickets(EventDate, [created, added, removed]);
        Assert.Empty(Assert.Single(afterRemove.Items).AccountIDs);
    }

    [Fact]
    public void ProposalCreateModify_PreservesAllocationsByAccount()
    {
        var created = CreateTicket();
        var added = AddAccount(created);
        var tickets = new Tickets(EventDate, [created, added]);
        var proposal = TicketEventBuilder.CreateProposal(
            new TicketProposalRequest(UserID, EventDate, "Create proposal", created.TicketNumber, new Price(10m), new TransactionQuantity(100m), null, [new TicketProposalAllocation(AccountID, 7m)]),
            tickets).Value!;
        tickets = new Tickets(EventDate, [created, added, proposal]);
        var modified = TicketEventBuilder.ModifyProposal(
            new TicketProposalRequest(UserID, EventDate, "Modify proposal", created.TicketNumber, new Price(11m), new TransactionQuantity(110m), Alpha3Builder.Create("USD"), [new TicketProposalAllocation(AccountID, 8m)]),
            tickets).Value!;

        var ticket = Assert.Single(new Tickets(EventDate, [created, added, proposal, modified]).Items);
        Assert.Equal(TicketStage.Proposal, ticket.Stage);
        Assert.Equal(TicketDecision.InProgress, ticket.ProposalDecision);
        Assert.Equal(11m, ticket.ProposalTargetPrice?.Amount);
        Assert.Equal(110m, ticket.ProposalTotalAmount?.Value);
        Assert.Equal(Alpha3Builder.Create("USD"), ticket.TradeCurrency);
        Assert.Equal(8m, Assert.Single(ticket.ProposalAllocations).Quantity);
    }

    [Fact]
    public void ProposalDecisionRequest_MarksProposalPendingApproval()
    {
        var created = CreateTicket();
        var added = AddAccount(created);
        var proposal = TicketEventBuilder.CreateProposal(
            new TicketProposalRequest(UserID, EventDate, "Create proposal", created.TicketNumber, new Price(10m), new TransactionQuantity(100m), null, [new TicketProposalAllocation(AccountID, 7m)]),
            new Tickets(EventDate, [created, added])).Value!;

        var requested = TicketEventBuilder.RequestProposalDecision(new TicketApprovalRequest(UserID, EventDate, "Request decision", TicketOne), new Tickets(EventDate, [created, added, proposal])).Value!;

        var ticket = Assert.Single(new Tickets(EventDate, [created, added, proposal, requested]).Items);
        Assert.Equal(TicketStage.Proposal, ticket.Stage);
        Assert.Equal(TicketDecision.PendingApproval, ticket.ProposalDecision);
    }

    [Fact]
    public void ProposalApproval_MovesToTradeAndDeclineReturnsToInProgressProposal()
    {
        var events = CreateProposalEvents();
        var approved = TicketEventBuilder.ApproveProposal(new TicketApprovalRequest(UserID, EventDate, "Approve", TicketOne), new Tickets(EventDate, events)).Value!;
        var notApprovedEvents = CreateProposalEvents(TicketTwo);
        var notApproved = TicketEventBuilder.NotApproveProposal(new TicketApprovalRequest(UserID, EventDate, "Not approve", TicketTwo), new Tickets(EventDate, notApprovedEvents)).Value!;

        var approvedTicket = Assert.Single(new Tickets(EventDate, [.. events, approved]).Items);
        var notApprovedTicket = Assert.Single(new Tickets(EventDate, [.. notApprovedEvents, notApproved]).Items);

        Assert.Equal(TicketStage.Trade, approvedTicket.Stage);
        Assert.Equal(TicketDecision.Approved, approvedTicket.ProposalDecision);
        Assert.Equal(TicketDecision.InProgress, approvedTicket.TradeDecision);
        Assert.Equal(TicketStage.Proposal, notApprovedTicket.Stage);
        Assert.Equal(TicketDecision.InProgress, notApprovedTicket.ProposalDecision);
        Assert.True(notApprovedTicket.IsActive);
    }

    [Fact]
    public void ProposalApproval_RejectsWithoutPendingProposal()
    {
        var created = CreateTicket();
        var added = AddAccount(created);

        var result = TicketEventBuilder.ApproveProposal(new TicketApprovalRequest(UserID, EventDate, "Approve", TicketOne), new Tickets(EventDate, [created, added]));

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, error => error.Contains("pending", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ValidationErrors, error => error.Contains("allocations", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProposalTextSetEvents_OnlyApplyAtProposalStage()
    {
        var events = CreateProposalEvents();
        var proposalReason = TicketEventBuilder.SetProposalReason(
            new TicketTextSetRequest(UserID, EventDate, "Set proposal reason", TicketOne, "Client rebalance"),
            new Tickets(EventDate, events)).Value!;
        var proposalAllocation = TicketEventBuilder.SetProposalAllocation(
            new TicketTextSetRequest(UserID, EventDate, "Set proposal allocation", TicketOne, "Allocate by target weight"),
            new Tickets(EventDate, [.. events, proposalReason])).Value!;
        var approved = TicketEventBuilder.ApproveProposal(
            new TicketApprovalRequest(UserID, EventDate, "Approve", TicketOne),
            new Tickets(EventDate, [.. events, proposalReason, proposalAllocation])).Value!;

        var ticket = Assert.Single(new Tickets(EventDate, [.. events, proposalReason, proposalAllocation, approved]).Items);
        Assert.Equal("Client rebalance", ticket.ProposalReason);
        Assert.Equal("Allocate by target weight", ticket.ProposalAllocation);

        var result = TicketEventBuilder.SetProposalReason(
            new TicketTextSetRequest(UserID, EventDate, "Set proposal reason", TicketOne, "Late change"),
            new Tickets(EventDate, [.. events, proposalReason, proposalAllocation, approved]));

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, error => error.Contains("Proposal stage", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TradeAndFills_RebuildCorrectly()
    {
        var events = CreateProposalEvents();
        var approved = TicketEventBuilder.ApproveProposal(new TicketApprovalRequest(UserID, EventDate, "Approve", TicketOne), new Tickets(EventDate, events)).Value!;
        var trade = TicketEventBuilder.CreateTrade(
            new TicketTradeRequest(UserID, EventDate, "Create trade", TicketOne, new Price(12m), [new TicketTradeAllocation(AccountID, 9m, 108m)]),
            new Tickets(EventDate, [.. events, approved])).Value!;
        var fill = TicketEventBuilder.AddFill(
            new TicketTradeFillRequest(UserID, EventDate, "Add fill", TicketOne, FillID, BrokerLEI, 12m, 9m, "Done"),
            new Tickets(EventDate, [.. events, approved, trade])).Value!;
        var modified = TicketEventBuilder.ModifyFill(
            new TicketTradeFillRequest(UserID, EventDate, "Modify fill", TicketOne, FillID, BrokerLEI, 12.5m, 8m, "Partial"),
            new Tickets(EventDate, [.. events, approved, trade, fill])).Value!;
        var removed = TicketEventBuilder.RemoveFill(
            new TicketTradeFillRemovedRequest(UserID, EventDate, "Remove fill", TicketOne, FillID),
            new Tickets(EventDate, [.. events, approved, trade, fill, modified])).Value!;

        var withFill = Assert.Single(new Tickets(EventDate, [.. events, approved, trade, fill, modified]).Items);
        var withoutFill = Assert.Single(new Tickets(EventDate, [.. events, approved, trade, fill, modified, removed]).Items);

        Assert.Equal(TicketStage.Trade, withFill.Stage);
        Assert.Equal(TicketDecision.PendingApproval, withFill.TradeDecision);
        var rebuiltFill = Assert.Single(withFill.Fills);
        Assert.Equal(BrokerLEI, rebuiltFill.BrokerLEI);
        Assert.Equal(12.5m, rebuiltFill.Price);
        Assert.Empty(withoutFill.Fills);
        Assert.Equal(TicketDecision.PendingApproval, withoutFill.TradeDecision);
    }

    [Fact]
    public void TradeApproval_MarksTicketCompleteAndNotApprovedStaysInTrade()
    {
        var events = CreateTradeEvents();
        var approved = TicketEventBuilder.ApproveTrade(new TicketApprovalRequest(UserID, EventDate, "Approve trade", TicketOne), new Tickets(EventDate, events)).Value!;
        var notApprovedEvents = CreateTradeEvents(TicketTwo);
        var notApproved = TicketEventBuilder.NotApproveTrade(new TicketApprovalRequest(UserID, EventDate, "Do not approve trade", TicketTwo), new Tickets(EventDate, notApprovedEvents)).Value!;

        var ticket = Assert.Single(new Tickets(EventDate, [.. events, approved]).Items);
        var notApprovedTicket = Assert.Single(new Tickets(EventDate, [.. notApprovedEvents, notApproved]).Items);

        Assert.Equal(TicketStage.Completed, ticket.Stage);
        Assert.Equal(TicketDecision.Approved, ticket.TradeDecision);
        Assert.False(ticket.IsActive);
        Assert.Equal(TicketStage.Trade, notApprovedTicket.Stage);
        Assert.Equal(TicketDecision.NotApproved, notApprovedTicket.TradeDecision);
        Assert.True(notApprovedTicket.IsActive);
    }

    [Fact]
    public void TradeApproval_RejectsWithoutPendingTrade()
    {
        var events = CreateProposalEvents();
        var proposalApproved = TicketEventBuilder.ApproveProposal(new TicketApprovalRequest(UserID, EventDate, "Approve proposal", TicketOne), new Tickets(EventDate, events)).Value!;

        var result = TicketEventBuilder.ApproveTrade(new TicketApprovalRequest(UserID, EventDate, "Approve trade", TicketOne), new Tickets(EventDate, [.. events, proposalApproved]));

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, error => error.Contains("pending", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ValidationErrors, error => error.Contains("allocations", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TradeTextSetEvents_OnlyApplyAtTradeStage()
    {
        var proposalEvents = CreateProposalEvents();
        var tradeResult = TicketEventBuilder.SetTradeInstructionNotes(
            new TicketTextSetRequest(UserID, EventDate, "Set trade instructions", TicketOne, "Work VWAP"),
            new Tickets(EventDate, proposalEvents));

        Assert.False(tradeResult.IsValid);
        Assert.Contains(tradeResult.ValidationErrors, error => error.Contains("Trade stage", StringComparison.OrdinalIgnoreCase));

        var tradeEvents = CreateTradeEvents();
        var instructions = TicketEventBuilder.SetTradeInstructionNotes(
            new TicketTextSetRequest(UserID, EventDate, "Set trade instructions", TicketOne, "Work VWAP"),
            new Tickets(EventDate, tradeEvents)).Value!;
        var progress = TicketEventBuilder.SetTradeProgressNotes(
            new TicketTextSetRequest(UserID, EventDate, "Set trade progress", TicketOne, "Half filled"),
            new Tickets(EventDate, [.. tradeEvents, instructions])).Value!;

        var ticket = Assert.Single(new Tickets(EventDate, [.. tradeEvents, instructions, progress]).Items);
        Assert.Equal("Work VWAP", ticket.TradeInstructionNotes);
        Assert.Equal("Half filled", ticket.TradeProgressNotes);
    }

    [Fact]
    public void Cancellation_WorksBeforeCompletionAndRejectsAfterCompletion()
    {
        var events = CreateProposalEvents();
        var cancelled = TicketEventBuilder.Cancel(new TicketCancellationRequest(UserID, EventDate, "Cancel", TicketOne), new Tickets(EventDate, events)).Value!;

        var ticket = Assert.Single(new Tickets(EventDate, [.. events, cancelled]).Items);
        Assert.Equal(TicketStage.Cancelled, ticket.Stage);
        Assert.False(ticket.IsActive);

        var completedEvents = CreateTradeEvents();
        var tradeApproved = TicketEventBuilder.ApproveTrade(new TicketApprovalRequest(UserID, EventDate, "Approve trade", TicketOne), new Tickets(EventDate, completedEvents)).Value!;
        var result = TicketEventBuilder.Cancel(new TicketCancellationRequest(UserID, EventDate, "Cancel", TicketOne), new Tickets(EventDate, [.. completedEvents, tradeApproved]));

        Assert.False(result.IsValid);
        Assert.Contains("complete", result.ValidationErrors[0]);
    }

    private static readonly UserID UserID = new(Guid.Parse("6d21c6ec-2cf0-430d-987b-6c32c1903538"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-10));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-9));
    private static readonly AccountID AccountID = AccountIDBuilder.Create();
    private static readonly InstrumentID InstrumentID = InstrumentIDBuilder.Create();
    private static readonly TicketNumber TicketOne = new(1);
    private static readonly TicketNumber TicketTwo = new(2);
    private static readonly Guid FillID = Guid.NewGuid();
    private static readonly LegalEntityIdentifier BrokerLEI = new("5493001KJTIIGC8Y1R12");

    private static TicketCreatedRequest CreateRequest(TicketSide side) =>
        new(UserID, EventDate, "Create ticket", side, InstrumentID);

    private static TicketCreatedEvent CreateTicket(TicketNumber? ticketNumber = null) =>
        TicketEventBuilder.Create(CreateRequest(TicketSide.Buy), ticketNumber ?? TicketOne, CreateInstruments()).Value!;

    private static TicketAccountAddedEvent AddAccount(TicketCreatedEvent created) =>
        TicketEventBuilder.AddAccount(new TicketAccountRequest(UserID, EventDate, "Add account", created.TicketNumber, AccountID), new Tickets(EventDate, [created]), CreateAccounts()).Value!;

    private static List<ITicket> CreateProposalEvents(TicketNumber? ticketNumber = null)
    {
        var created = CreateTicket(ticketNumber ?? TicketOne);
        var added = AddAccount(created);
        var proposal = TicketEventBuilder.CreateProposal(
            new TicketProposalRequest(UserID, EventDate, "Create proposal", created.TicketNumber, new Price(10m), new TransactionQuantity(100m), null, [new TicketProposalAllocation(AccountID, 7m)]),
            new Tickets(EventDate, [created, added])).Value!;
        var requested = TicketEventBuilder.RequestProposalDecision(
            new TicketApprovalRequest(UserID, EventDate, "Request proposal decision", created.TicketNumber),
            new Tickets(EventDate, [created, added, proposal])).Value!;

        return [created, added, proposal, requested];
    }

    private static List<ITicket> CreateTradeEvents(TicketNumber? ticketNumber = null)
    {
        var number = ticketNumber ?? TicketOne;
        var events = CreateProposalEvents(number);
        var proposalApproved = TicketEventBuilder.ApproveProposal(new TicketApprovalRequest(UserID, EventDate, "Approve proposal", number), new Tickets(EventDate, events)).Value!;
        var trade = TicketEventBuilder.CreateTrade(
            new TicketTradeRequest(UserID, EventDate, "Create trade", number, new Price(12m), [new TicketTradeAllocation(AccountID, 9m, 108m)]),
            new Tickets(EventDate, [.. events, proposalApproved])).Value!;

        return [.. events, proposalApproved, trade];
    }

    private static Accounts CreateAccounts()
    {
        var created = AccountCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            EventDate,
            AuditDate,
            "Create account",
            AccountID,
            "General",
            "General Account",
            Alpha3Builder.Create("GBP"),
            true).Value!;

        return new Accounts(EventDate, AuditDate, [created]);
    }

    private static Instruments CreateInstruments()
    {
        var created = InstrumentCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            EventDate,
            AuditDate,
            "Create instrument",
            InstrumentID,
            "Vodafone",
            "Vodafone Group plc",
            ExchangeBuilder.Create("XLON"),
            CFIBuilder.Create("ESVUFR"),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create("GBP")).Value!;

        return new Instruments(EventDate, AuditDate, [created]);
    }
}
