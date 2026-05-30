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
            new TicketProposalRequest(UserID, EventDate, "Create proposal", created.TicketNumber, 10m, 100m, [new TicketProposalAllocation(AccountID, 7m)]),
            tickets).Value!;
        tickets = new Tickets(EventDate, [created, added, proposal]);
        var modified = TicketEventBuilder.ModifyProposal(
            new TicketProposalRequest(UserID, EventDate, "Modify proposal", created.TicketNumber, 11m, 110m, [new TicketProposalAllocation(AccountID, 8m)]),
            tickets).Value!;

        var ticket = Assert.Single(new Tickets(EventDate, [created, added, proposal, modified]).Items);
        Assert.Equal(TicketStatus.Proposal, ticket.Status);
        Assert.Equal(11m, ticket.ProposalTargetPrice);
        Assert.Equal(8m, Assert.Single(ticket.ProposalAllocations).Quantity);
    }

    [Fact]
    public void ProposalApproval_ChangesStatusAndNotApprovedStaysActive()
    {
        var events = CreateProposalEvents();
        var approved = TicketEventBuilder.ApproveProposal(new TicketApprovalRequest(UserID, EventDate, "Approve", TicketOne), new Tickets(EventDate, events)).Value!;
        var notApprovedEvents = CreateProposalEvents(TicketTwo);
        var notApproved = TicketEventBuilder.NotApproveProposal(new TicketApprovalRequest(UserID, EventDate, "Not approve", TicketTwo), new Tickets(EventDate, notApprovedEvents)).Value!;

        var approvedTicket = Assert.Single(new Tickets(EventDate, [.. events, approved]).Items);
        var notApprovedTicket = Assert.Single(new Tickets(EventDate, [.. notApprovedEvents, notApproved]).Items);

        Assert.Equal(TicketStatus.ProposalApproved, approvedTicket.Status);
        Assert.Equal(TicketStatus.ProposalNotApproved, notApprovedTicket.Status);
        Assert.True(notApprovedTicket.IsActive);
    }

    [Fact]
    public void TradeAndFills_RebuildCorrectly()
    {
        var events = CreateProposalEvents();
        var approved = TicketEventBuilder.ApproveProposal(new TicketApprovalRequest(UserID, EventDate, "Approve", TicketOne), new Tickets(EventDate, events)).Value!;
        var trade = TicketEventBuilder.CreateTrade(
            new TicketTradeRequest(UserID, EventDate, "Create trade", TicketOne, 12m, [new TicketTradeAllocation(AccountID, 9m, 108m)]),
            new Tickets(EventDate, [.. events, approved])).Value!;
        var fill = TicketEventBuilder.AddFill(
            new TicketTradeFillRequest(UserID, EventDate, "Add fill", TicketOne, FillID, 12m, 9m, "Done"),
            new Tickets(EventDate, [.. events, approved, trade])).Value!;
        var modified = TicketEventBuilder.ModifyFill(
            new TicketTradeFillRequest(UserID, EventDate, "Modify fill", TicketOne, FillID, 12.5m, 8m, "Partial"),
            new Tickets(EventDate, [.. events, approved, trade, fill])).Value!;
        var removed = TicketEventBuilder.RemoveFill(
            new TicketTradeFillRemovedRequest(UserID, EventDate, "Remove fill", TicketOne, FillID),
            new Tickets(EventDate, [.. events, approved, trade, fill, modified])).Value!;

        var withFill = Assert.Single(new Tickets(EventDate, [.. events, approved, trade, fill, modified]).Items);
        var withoutFill = Assert.Single(new Tickets(EventDate, [.. events, approved, trade, fill, modified, removed]).Items);

        Assert.Equal(TicketStatus.Trade, withFill.Status);
        Assert.Equal(12.5m, Assert.Single(withFill.Fills).Price);
        Assert.Empty(withoutFill.Fills);
    }

    [Fact]
    public void TradeApproval_MarksTicketComplete()
    {
        var events = CreateTradeEvents();
        var approved = TicketEventBuilder.ApproveTrade(new TicketApprovalRequest(UserID, EventDate, "Approve trade", TicketOne), new Tickets(EventDate, events)).Value!;

        var ticket = Assert.Single(new Tickets(EventDate, [.. events, approved]).Items);

        Assert.Equal(TicketStatus.Completed, ticket.Status);
        Assert.False(ticket.IsActive);
    }

    [Fact]
    public void Cancellation_WorksBeforeCompletionAndRejectsAfterCompletion()
    {
        var events = CreateProposalEvents();
        var cancelled = TicketEventBuilder.Cancel(new TicketCancellationRequest(UserID, EventDate, "Cancel", TicketOne), new Tickets(EventDate, events)).Value!;

        var ticket = Assert.Single(new Tickets(EventDate, [.. events, cancelled]).Items);
        Assert.Equal(TicketStatus.Cancelled, ticket.Status);
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
            new TicketProposalRequest(UserID, EventDate, "Create proposal", created.TicketNumber, 10m, 100m, [new TicketProposalAllocation(AccountID, 7m)]),
            new Tickets(EventDate, [created, added])).Value!;

        return [created, added, proposal];
    }

    private static List<ITicket> CreateTradeEvents()
    {
        var events = CreateProposalEvents();
        var proposalApproved = TicketEventBuilder.ApproveProposal(new TicketApprovalRequest(UserID, EventDate, "Approve proposal", TicketOne), new Tickets(EventDate, events)).Value!;
        var trade = TicketEventBuilder.CreateTrade(
            new TicketTradeRequest(UserID, EventDate, "Create trade", TicketOne, 12m, [new TicketTradeAllocation(AccountID, 9m, 108m)]),
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
            Alpha2Builder.Create("GB")).Value!;

        return new Instruments(EventDate, AuditDate, [created]);
    }
}
