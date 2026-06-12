using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using System.Reflection;

namespace Test;

public sealed class TicketTradeApprovalTransactionBuilderTests
{
    [Fact]
    public void ApproveTradeWithTransactions_CreatesBuyCreditAssetAndDebitCash()
    {
        var result = Approve(TicketSide.Buy);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
        Assert.NotNull(result.Value);
        Assert.IsType<TicketTradeApprovedEvent>(result.Value!.ApprovalEvent);
        Assert.Empty(result.Value.HoldingEvents);

        var credit = Assert.IsType<TransactionCreditEvent>(result.Value.TransactionEvents[0]);
        var debit = Assert.IsType<TransactionDebitEvent>(result.Value.TransactionEvents[1]);

        Assert.Equal(AssetHoldingID, credit.HoldingID);
        Assert.Equal(InstrumentID, credit.InstrumentID);
        Assert.Equal(9m, credit.Quantity.Value);
        Assert.Equal(108m, credit.BookCost.Value);

        Assert.Equal(CashHoldingID, debit.HoldingID);
        Assert.Equal(CashInstrumentID, debit.InstrumentID);
        Assert.Equal(108m, debit.Quantity.Value);
        Assert.Equal(108m, debit.BookCost.Value);
        Assert.Equal(credit.EventSetID, debit.EventSetID);
    }

    [Fact]
    public void ApproveTradeWithTransactions_CreatesSellCreditCashAndDebitAsset()
    {
        var result = Approve(TicketSide.Sell);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!.HoldingEvents);

        var credit = Assert.IsType<TransactionCreditEvent>(result.Value.TransactionEvents[0]);
        var debit = Assert.IsType<TransactionDebitEvent>(result.Value.TransactionEvents[1]);

        Assert.Equal(CashHoldingID, credit.HoldingID);
        Assert.Equal(CashInstrumentID, credit.InstrumentID);
        Assert.Equal(108m, credit.Quantity.Value);
        Assert.Equal(108m, credit.BookCost.Value);

        Assert.Equal(AssetHoldingID, debit.HoldingID);
        Assert.Equal(InstrumentID, debit.InstrumentID);
        Assert.Equal(9m, debit.Quantity.Value);
        Assert.Equal(108m, debit.BookCost.Value);
    }

    [Fact]
    public void ApproveTradeWithTransactions_AllowsSettlementDateOnTradeDate()
    {
        var settlementDate = SettlementDateTimeBuilder.Create(EventDate.Value.Date);
        var result = TicketEventBuilder.ApproveTradeWithTransactions(
            CreateApprovalRequest(),
            CreatePendingTickets(settlementDate: settlementDate),
            CreateAccounts(),
            CreateInstruments(),
            CreateHoldingEvents());

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
        Assert.All(result.Value!.TransactionEvents, item => Assert.Equal(settlementDate, item.SettlementDateTime));
    }

    [Fact]
    public void ApproveTradeWithTransactions_RejectsMissingFillsAndMismatchedTotals()
    {
        var missing = TicketEventBuilder.ApproveTradeWithTransactions(
            CreateApprovalRequest(),
            CreatePendingTickets(includeFill: false),
            CreateAccounts(),
            CreateInstruments(),
            CreateHoldingEvents());
        var mismatched = TicketEventBuilder.ApproveTradeWithTransactions(
            CreateApprovalRequest(),
            CreatePendingTickets(fillQuantity: 8m, fillBookCost: 107m),
            CreateAccounts(),
            CreateInstruments(),
            CreateHoldingEvents());

        Assert.False(missing.IsValid);
        Assert.Contains("Trade fills are required before trade approval.", missing.ValidationErrors);
        Assert.False(mismatched.IsValid);
        Assert.Contains(mismatched.ValidationErrors, error => error.Contains("Fills must sum", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mismatched.ValidationErrors, error => error.Contains("Fills book cost", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApproveTradeWithTransactions_CreatesMissingAssetHoldingAndRejectsAmbiguousAssetHolding()
    {
        var missing = TicketEventBuilder.ApproveTradeWithTransactions(
            CreateApprovalRequest(),
            CreatePendingTickets(),
            CreateAccounts(),
            CreateInstruments(),
            CreateHoldingEvents(includeAsset: false));
        var ambiguous = TicketEventBuilder.ApproveTradeWithTransactions(
            CreateApprovalRequest(),
            CreatePendingTickets(),
            CreateAccounts(),
            CreateInstruments(),
            CreateHoldingEvents(includeSecondAsset: true));

        Assert.True(missing.IsValid, string.Join(Environment.NewLine, missing.ValidationErrors));
        Assert.NotNull(missing.Value);
        var createdHolding = Assert.Single(missing.Value!.HoldingEvents);
        Assert.Equal(AccountID, createdHolding.AccountID);
        Assert.Equal(InstrumentID, createdHolding.InstrumentID);
        Assert.Equal("Vodafone", createdHolding.Name);
        Assert.True(createdHolding.Active);
        Assert.False(createdHolding.Default);
        Assert.Equal("Create asset holding for ticket approval", createdHolding.Reason);
        var createdAssetCredit = Assert.IsType<TransactionCreditEvent>(missing.Value.TransactionEvents[0]);
        Assert.Equal(createdHolding.HoldingID, createdAssetCredit.HoldingID);

        Assert.False(ambiguous.IsValid);
        Assert.Contains(ambiguous.ValidationErrors, error => error.Contains("Multiple active asset holdings", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApprovedTradeTransactions_AffectHoldingPositionsByEventAndSettlementDate()
    {
        var holdingEvents = CreateHoldingEvents();
        var holdings = new Holdings(EventDate, AuditDate, holdingEvents);
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var result = Approve(TicketSide.Buy, holdingEvents, instruments, accounts);
        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));

        var betweenExecutionAndSettlement = EventDateTimeBuilder.Create(EventDate.Value.AddHours(1));
        var eventDatePositions = new HoldingPositions(betweenExecutionAndSettlement, AuditDateTimeBuilder.Create(DateTime.UtcNow), holdings, accounts, instruments, result.Value!.TransactionEvents.Cast<ITransactionEvent>().ToList());
        var settlementDatePositions = new HoldingPositions(betweenExecutionAndSettlement, AuditDateTimeBuilder.Create(DateTime.UtcNow), holdings, accounts, instruments, result.Value.TransactionEvents.Cast<ITransactionEvent>().ToList(), holdingDateBasis: HoldingDateBasis.SettlementDateTime);
        var settledPositions = new HoldingPositions(EventDateTimeBuilder.Create(SettlementDate.Value), AuditDateTimeBuilder.Create(DateTime.UtcNow), holdings, accounts, instruments, result.Value.TransactionEvents.Cast<ITransactionEvent>().ToList(), holdingDateBasis: HoldingDateBasis.SettlementDateTime);

        Assert.Contains(eventDatePositions.Items, position => position.HoldingID == AssetHoldingID && position.Quantity == 9m && position.BookCost == 108m);
        Assert.Contains(eventDatePositions.Items, position => position.HoldingID == CashHoldingID && position.Quantity == -108m && position.BookCost == -108m);
        Assert.Empty(settlementDatePositions.Items);
        Assert.Contains(settledPositions.Items, position => position.HoldingID == AssetHoldingID && position.Quantity == 9m && position.BookCost == 108m);
        Assert.Contains(settledPositions.Items, position => position.HoldingID == CashHoldingID && position.Quantity == -108m && position.BookCost == -108m);
    }

    [Fact]
    public void ApprovedTradeTransactions_AffectAutoCreatedAssetHoldingPosition()
    {
        var existingHoldingEvents = CreateHoldingEvents(includeAsset: false);
        var accounts = CreateAccounts();
        var instruments = CreateInstruments();
        var result = Approve(TicketSide.Buy, existingHoldingEvents, instruments, accounts);
        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
        Assert.NotNull(result.Value);

        var createdHolding = Assert.Single(result.Value!.HoldingEvents);
        var holdings = new Holdings(EventDate, [.. existingHoldingEvents, .. result.Value.HoldingEvents]);
        var positions = new HoldingPositions(
            EventDate,
            AuditDateTimeBuilder.Create(DateTime.UtcNow),
            holdings,
            accounts,
            instruments,
            result.Value.TransactionEvents.Cast<ITransactionEvent>().ToList());

        Assert.Contains(positions.Items, position => position.HoldingID == createdHolding.HoldingID && position.Quantity == 9m && position.BookCost == 108m);
    }

    private static Result<TicketTradeApprovalTransactionResult> Approve(TicketSide side, IReadOnlyList<IHoldingEvent>? holdingEvents = null, Instruments? instruments = null, Accounts? accounts = null) =>
        TicketEventBuilder.ApproveTradeWithTransactions(
            CreateApprovalRequest(),
            CreatePendingTickets(side),
            accounts ?? CreateAccounts(),
            instruments ?? CreateInstruments(),
            holdingEvents ?? CreateHoldingEvents());

    private static TicketTradeApprovalRequest CreateApprovalRequest() =>
        new(UserID, EventDate, "Approve trade", TicketNumber);

    private static Tickets CreatePendingTickets(TicketSide side = TicketSide.Buy, bool includeFill = true, decimal fillQuantity = 9m, decimal fillBookCost = 108m, SettlementDateTime? settlementDate = null)
    {
        var created = TicketEventBuilder.Create(
            new TicketCreatedRequest(UserID, EventDate, "Create ticket", side, InstrumentID),
            TicketNumber,
            CreateInstruments()).Value!;
        var accountAdded = TicketEventBuilder.AddAccount(
            new TicketAccountRequest(UserID, EventDate, "Add account", TicketNumber, AccountID),
            new Tickets(EventDate, [created]),
            CreateAccounts()).Value!;
        var proposal = TicketEventBuilder.CreateProposal(
            new TicketProposalRequest(UserID, EventDate, "Create proposal", TicketNumber, new Price(10m), null, [new TicketProposalAllocation(AccountID, 9m)]),
            new Tickets(EventDate, [created, accountAdded])).Value!;
        var proposalRequested = TicketEventBuilder.RequestProposalDecision(
            new TicketApprovalRequest(UserID, EventDate, "Request proposal decision", TicketNumber),
            new Tickets(EventDate, [created, accountAdded, proposal])).Value!;
        var proposalApproved = TicketEventBuilder.ApproveProposal(
            new TicketApprovalRequest(UserID, EventDate, "Approve proposal", TicketNumber),
            new Tickets(EventDate, [created, accountAdded, proposal, proposalRequested])).Value!;
        var trade = TicketEventBuilder.CreateTrade(
            new TicketTradeRequest(UserID, EventDate, "Create trade", TicketNumber, new Price(12m), EventDate, settlementDate ?? SettlementDate, [new TicketTradeAllocation(AccountID, 9m, 108m, CashHoldingID)]),
            new Tickets(EventDate, [created, accountAdded, proposal, proposalRequested, proposalApproved])).Value!;

        var events = new List<ITicket> { created, accountAdded, proposal, proposalRequested, proposalApproved, trade };
        if (includeFill)
        {
            var fill = TicketEventBuilder.AddFill(
                new TicketTradeFillRequest(UserID, EventDate, "Add fill", TicketNumber, Guid.CreateGuid7(), BrokerLEI, new Price(12m), fillQuantity, new TransactionBookCost(fillBookCost), "Done"),
                new Tickets(EventDate, events)).Value!;
            events.Add(fill);
        }

        var decisionRequested = includeFill
            ? TicketEventBuilder.RequestTradeDecision(
                new TicketApprovalRequest(UserID, EventDate, "Request trade decision", TicketNumber),
                new Tickets(EventDate, events),
                CreateHoldings(),
                CreateInstruments()).Value!
            : CreateTradeDecisionRequested();
        events.Add(decisionRequested);

        return new Tickets(EventDate, events);
    }

    private static Accounts CreateAccounts()
    {
        var account = AccountCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDate,
            "Create account",
            AccountID,
            "Trading",
            "Trading Account",
            Alpha3Builder.Create("GBP"),
            true).Value!;

        return new Accounts(EventDate, AuditDate, [account]);
    }

    private static Instruments CreateInstruments()
    {
        var asset = InstrumentCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
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
        var cash = InstrumentCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDate,
            "Create cash instrument",
            CashInstrumentID,
            "GBP cash",
            "Pound sterling cash",
            ExchangeBuilder.Create("XOFF"),
            CFIBuilder.Create("MRCXXX"),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create("GBP")).Value!;

        return new Instruments(EventDate, AuditDate, [asset, cash]);
    }

    private static Holdings CreateHoldings(bool includeAsset = true, bool includeSecondAsset = false)
    {
        return new Holdings(EventDate, AuditDate, CreateHoldingEvents(includeAsset, includeSecondAsset));
    }

    private static List<IHoldingEvent> CreateHoldingEvents(bool includeAsset = true, bool includeSecondAsset = false)
    {
        var events = new List<IHoldingEvent>();
        if (includeAsset)
            events.Add(CreateAssetHolding(AssetHoldingID, isDefault: !includeSecondAsset));
        if (includeSecondAsset)
            events.Add(CreateAssetHolding(SecondAssetHoldingID, isDefault: false));
        events.Add(CreateCashHolding());

        return events;
    }

    private static HoldingPositionAssetCreatedEvent CreateAssetHolding(HoldingID holdingID, bool isDefault) =>
        HoldingPositionAssetCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDate,
            "Create asset holding",
            holdingID,
            AccountID,
            InstrumentID,
            "Asset",
            true,
            isDefault).Value!;

    private static HoldingCashInvestableCreatedEvent CreateCashHolding() =>
        HoldingCashInvestableCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDate,
            "Create cash holding",
            CashHoldingID,
            AccountID,
            CashInstrumentID,
            "Investable GBP",
            true,
            true,
            "HSBC",
            "Investable",
            SortCodeBuilder.Create("12-34-56"),
            BankAccountNumberBuilder.Create("12345678"),
            BICBuilder.Create("HBUKGB4B"),
            IBANBuilder.Create("GB82WEST12345698765432")).Value!;

    private static TicketTradeDecisionRequestedEvent CreateTradeDecisionRequested() =>
        (TicketTradeDecisionRequestedEvent)Activator.CreateInstance(
            typeof(TicketTradeDecisionRequestedEvent),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                new EventID(Guid.CreateGuid7()),
                UserID,
                EventDate,
                AuditDateTimeBuilder.Create(),
                "Request trade decision",
                TicketNumber
            ],
            culture: null)!;

    private static readonly UserID UserID = new(Guid.Parse("4d77975e-8b18-4dc2-9836-5124235dc4f2"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));
    private static readonly SettlementDateTime SettlementDate = SettlementDateTimeBuilder.Create(EventDate.Value.Date.AddDays(1));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(EventDate.Value.AddMinutes(1));
    private static readonly AccountID AccountID = AccountIDBuilder.Create();
    private static readonly InstrumentID InstrumentID = InstrumentIDBuilder.Create();
    private static readonly InstrumentID CashInstrumentID = InstrumentIDBuilder.Create();
    private static readonly HoldingID AssetHoldingID = HoldingIDBuilder.Create();
    private static readonly HoldingID SecondAssetHoldingID = HoldingIDBuilder.Create();
    private static readonly HoldingID CashHoldingID = HoldingIDBuilder.Create();
    private static readonly TicketNumber TicketNumber = new(27);
    private static readonly LegalEntityIdentifier BrokerLEI = new("5493001KJTIIGC8Y1R12");
}
