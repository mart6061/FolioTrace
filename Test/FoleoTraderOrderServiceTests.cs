using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class FoleoTraderOrderServiceTests
{
    [Fact]
    public void ValidateOrder_RejectsMissingTicket()
    {
        var valuationDate = EventDateTimeBuilder.Create(new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc));
        var service = new FoleoTraderOrderService(null!);
        var request = new FoleoTraderOrderRequest(
            Constants.Initialisation.UserID,
            valuationDate,
            new TicketNumber(42),
            new LegalEntityIdentifier("5493001KJTIIGC8Y1R12"));

        var result = service.ValidateOrder(
            request,
            new Tickets(valuationDate, []),
            new Instruments(valuationDate, []),
            new FoleoTraderOrders(valuationDate, []));

        Assert.Null(result.Ticket);
        Assert.Contains("No matching ticket found for TicketNumber '42'.", result.ValidationErrors);
    }

    [Fact]
    public void NextWorkingDaySettlement_MovesFridayToMonday()
    {
        var friday = EventDateTimeBuilder.Create(new DateTime(2026, 7, 24, 15, 30, 0, DateTimeKind.Utc));

        var settlement = FoleoTraderOrderService.NextWorkingDaySettlement(friday);

        Assert.Equal(new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc), settlement.Value);
    }

    [Fact]
    public void ProposalQuantity_SumsAllocations()
    {
        var ticket = CreateTicket([
            new TicketProposalAllocation(AccountIDBuilder.Create(), 7m),
            new TicketProposalAllocation(AccountIDBuilder.Create(), 5m)
        ]);

        Assert.Equal(12m, FoleoTraderOrderService.ProposalQuantity(ticket));
    }

    private static Ticket CreateTicket(List<TicketProposalAllocation> allocations)
    {
        var valuationDate = EventDateTimeBuilder.Create(new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc));
        var asAt = AuditDateTimeBuilder.Create(new DateTime(2026, 7, 20, 12, 1, 0, DateTimeKind.Utc));
        return new Ticket
        {
            TicketNumber = new TicketNumber(42),
            Side = TicketSide.Buy,
            InstrumentID = InstrumentIDBuilder.Restore(Guid.NewGuid()),
            TradeCurrency = Alpha3Builder.Create("GBP"),
            Stage = TicketStage.Trade,
            ProposalDecision = TicketDecision.Approved,
            TradeDecision = TicketDecision.InProgress,
            TradeExecutionStatus = TicketTradeExecutionStatus.Ready,
            AccountIDs = allocations.Select(item => item.AccountID).ToList(),
            ProposalTargetPrice = new Price(10m),
            ProposalAllocations = allocations,
            ProposalReason = string.Empty,
            ProposalAllocation = string.Empty,
            TradeAllocations = [],
            Fills = [],
            TradeInstructionNotes = string.Empty,
            TradeProgressNotes = string.Empty,
            ValuationDateTime = valuationDate,
            AsOfDateTime = asAt,
            LastEventID = Constants.Initialisation.EmptyViewEventID,
            LastAuditDateTime = LastAuditDateTimeBuilder.Create(asAt.Value)
        };
    }
}
