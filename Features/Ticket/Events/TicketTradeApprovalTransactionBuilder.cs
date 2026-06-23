using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static partial class TicketEventBuilder
{
    public static Result<TicketTradeApprovalTransactionResult> ApproveTradeWithTransactions(
        TicketTradeApprovalRequest request,
        Tickets tickets,
        Accounts? accounts,
        Instruments? instruments,
        IReadOnlyList<IHoldingEvent> holdingEvents,
        FXRates? fxRates = null)
    {
        var messages = ValidateTradeApprovalTransactionRequest(request, tickets, accounts, instruments, holdingEvents, fxRates, out var ticket, out var holdingEventsToCreate, out var effectiveHoldings, out var legsByAllocation);
        if (messages.Count > 0 || ticket is null || effectiveHoldings is null)
            return Result<TicketTradeApprovalTransactionResult>.Invalid(messages);

        var approvalEvent = new TicketTradeApprovedEvent(
            NewEventID(),
            request.UserID,
            request.EventDateTime,
            AuditDateTimeBuilder.Create(),
            request.Reason,
            request.TicketNumber);

        var transactionEvents = new List<ITransactionMovementEvent>();
        foreach (var legs in legsByAllocation)
        {
            var transactionRequest = ticket.Side == TicketSide.Buy
                ? CreateTransactionSetRequest(request, ticket, legs.AssetLeg, legs.CashLeg)
                : CreateTransactionSetRequest(request, ticket, legs.CashLeg, legs.AssetLeg);

            var transactionResult = TransactionBuilder.Create(transactionRequest, effectiveHoldings, accounts);
            if (!transactionResult.IsValid || transactionResult.Value is null)
                return Result<TicketTradeApprovalTransactionResult>.Invalid(transactionResult.ValidationErrors);

            transactionEvents.AddRange(transactionResult.Value);
        }

        return Result<TicketTradeApprovalTransactionResult>.Success(new TicketTradeApprovalTransactionResult(approvalEvent, holdingEventsToCreate, transactionEvents));
    }

    private static List<string> ValidateTradeApprovalTransactionRequest(
        TicketTradeApprovalRequest request,
        Tickets tickets,
        Accounts? accounts,
        Instruments? instruments,
        IReadOnlyList<IHoldingEvent>? holdingEvents,
        FXRates? fxRates,
        out Ticket? ticket,
        out IReadOnlyList<HoldingPositionAssetCreatedEvent> holdingEventsToCreate,
        out Holdings? effectiveHoldings,
        out IReadOnlyList<TicketTradeApprovalTransactionLegs> legsByAllocation)
    {
        var messages = ValidateTicketMutation(request.UserID, request.EventDateTime, request.Reason, request.TicketNumber, tickets, out ticket);
        holdingEventsToCreate = [];
        effectiveHoldings = null;
        legsByAllocation = [];

        if (accounts is null)
            messages.Add("Accounts are required.");
        if (instruments is null)
            messages.Add("Instruments are required.");
        if (holdingEvents is null || holdingEvents.Count == 0)
            messages.Add("Holdings are required.");

        if (ticket is null)
            return messages;

        var currentHoldings = TryBuildHoldings(request.EventDateTime, holdingEvents, messages);

        if (ticket.Stage != TicketStage.Trade)
            messages.Add("Ticket must be in Trade stage.");
        if (ticket.TradeDecision != TicketDecision.PendingApproval)
            messages.Add("Trade decision must be pending.");
        if (ticket.TradeAllocations.Count == 0)
            messages.Add("Trade allocations are required before trade approval.");
        else
            ValidateTradeAllocations(ticket.TradeAllocations, ticket, currentHoldings, instruments, messages);
        ValidateTradeDates(ticket.TradeDateTime, ticket.SettlementDateTime, messages);

        ValidateTradeFills(ticket, messages);

        if (messages.Count > 0 || currentHoldings is null || accounts is null || instruments is null || holdingEvents is null)
            return messages;

        var createdHoldingEvents = new List<HoldingPositionAssetCreatedEvent>();
        var legs = new List<TicketTradeApprovalTransactionLegs>();
        foreach (var allocation in ticket.TradeAllocations)
        {
            var assetHolding = /*  */ResolveAssetHolding(allocation, ticket, currentHoldings, messages);
            if (assetHolding is null)
            {
                var createdHolding = ResolveCreatedAssetHolding(allocation, ticket, request, accounts, instruments, currentHoldings, createdHoldingEvents, messages);
                if (createdHolding is not null && createdHoldingEvents.All(@event => @event.EventID != createdHolding.EventID))
                    createdHoldingEvents.Add(createdHolding);
            }
        }

        if (messages.Count > 0)
            return messages;

        if (createdHoldingEvents.Count > 0)
            currentHoldings = new Holdings(request.EventDateTime!, [.. holdingEvents, .. createdHoldingEvents]);

        foreach (var allocation in ticket.TradeAllocations)
        {
            var assetHolding = ResolveAssetHolding(allocation, ticket, currentHoldings, messages);
            var cashHolding = allocation.CashHoldingID is null
                ? null
                : currentHoldings.Items.SingleOrDefault(holding => holding.HoldingID == allocation.CashHoldingID);

            if (assetHolding is null || cashHolding is null)
                continue;

            var account = accounts.Items.SingleOrDefault(item => item.AccountID == allocation.AccountID);
            var cashInstrument = instruments.Items.SingleOrDefault(item => item.InstrumentID == cashHolding.InstrumentID);
            if (account is null || cashInstrument is null)
                continue;

            var bookCost = ResolveBookCost(allocation, account, cashInstrument.PriceCurrency, fxRates, messages);
            if (bookCost is null)
                continue;

            legs.Add(new TicketTradeApprovalTransactionLegs(
                new TransactionRequest(
                    assetHolding.HoldingID,
                    ticket.InstrumentID,
                    allocation.AccountID,
                    new TransactionQuantity(allocation.Quantity),
                    new TransactionLocalCost(allocation.SettlementAmount),
                    cashInstrument.PriceCurrency,
                    bookCost.BookCost,
                    bookCost.Source,
                    bookCost.Estimated),
                new TransactionRequest(
                    cashHolding.HoldingID,
                    cashHolding.InstrumentID,
                    allocation.AccountID,
                    new TransactionQuantity(allocation.SettlementAmount),
                    new TransactionLocalCost(allocation.SettlementAmount),
                    cashInstrument.PriceCurrency,
                    bookCost.BookCost,
                    bookCost.Source,
                    bookCost.Estimated)));
        }

        holdingEventsToCreate = createdHoldingEvents;
        effectiveHoldings = currentHoldings;
        legsByAllocation = legs;
        return messages;
    }

    private static Holdings? TryBuildHoldings(EventDateTime? eventDateTime, IReadOnlyList<IHoldingEvent>? holdingEvents, List<string> messages)
    {
        if (eventDateTime is null || holdingEvents is null || holdingEvents.Count == 0)
            return null;

        try
        {
            return new Holdings(eventDateTime, holdingEvents.ToList());
        }
        catch (ArgumentException exception)
        {
            messages.Add(exception.Message);
            return null;
        }
    }

    private static void ValidateTradeFills(Ticket ticket, List<string> messages)
    {
        if (ticket.Fills.Count == 0)
        {
            messages.Add("Trade fills are required before trade approval.");
            return;
        }

        var fillQuantity = ticket.Fills.Sum(fill => fill.Quantity);
        var allocationQuantity = ticket.TradeAllocations.Sum(allocation => allocation.Quantity);
        if (fillQuantity != allocationQuantity)
            messages.Add($"Fills must sum to trade allocation quantity {FormatValidationNumber(allocationQuantity)}. Current total is {FormatValidationNumber(fillQuantity)}.");

        var fillSettlementAmount = ticket.Fills.Sum(fill => fill.SettlementAmount.Value);
        var allocationSettlementAmount = ticket.TradeAllocations.Sum(allocation => allocation.SettlementAmount);
        if (fillSettlementAmount != allocationSettlementAmount)
            messages.Add($"Fills settlement amount must sum to trade allocation settlement amount {FormatValidationNumber(allocationSettlementAmount)}. Current total is {FormatValidationNumber(fillSettlementAmount)}.");
    }

    private static HoldingPositionAsset? ResolveAssetHolding(TicketTradeAllocation allocation, Ticket ticket, Holdings holdings, List<string> messages)
    {
        var matches = holdings.Items
            .OfType<HoldingPositionAsset>()
            .Where(holding =>
                holding.Active &&
                holding.AccountID == allocation.AccountID &&
                holding.InstrumentID == ticket.InstrumentID)
            .ToList();

        if (matches.Count == 0)
            return null;

        var defaultHolding = matches.FirstOrDefault(holding => holding.Default);
        if (defaultHolding is not null)
            return defaultHolding;

        if (matches.Count == 1)
            return matches[0];

        messages.Add($"Multiple active asset holdings found for AccountID '{allocation.AccountID}' and InstrumentID '{ticket.InstrumentID}'. Set one as default.");
        return null;
    }

    private static HoldingPositionAssetCreatedEvent? ResolveCreatedAssetHolding(
        TicketTradeAllocation allocation,
        Ticket ticket,
        TicketTradeApprovalRequest request,
        Accounts accounts,
        Instruments instruments,
        Holdings holdings,
        IReadOnlyList<HoldingPositionAssetCreatedEvent> createdHoldingEvents,
        List<string> messages)
    {
        var existingCreated = createdHoldingEvents
            .FirstOrDefault(holding => holding.AccountID == allocation.AccountID && holding.InstrumentID == ticket.InstrumentID);
        if (existingCreated is not null)
            return existingCreated;

        var instrument = instruments.Items.SingleOrDefault(item => item.InstrumentID == ticket.InstrumentID);
        var name = string.IsNullOrWhiteSpace(instrument?.Name) ? "Asset" : instrument.Name;
        var result = HoldingPositionAssetCreatedEventBuilder.Create(
            new HoldingPositionAssetCreatedRequest(
                request.UserID,
                ticket.TradeDateTime ?? request.EventDateTime,
                "Create asset holding for ticket approval",
                null,
                allocation.AccountID,
                ticket.InstrumentID,
                name,
                true,
                false),
            accounts,
            instruments,
            holdings);

        if (result.IsValid && result.Value is not null)
            return result.Value;

        messages.AddRange(result.ValidationErrors);
        return null;
    }

    private static TransactionSetRequest CreateTransactionSetRequest(
        TicketTradeApprovalRequest request,
        Ticket ticket,
        TransactionRequest credit,
        TransactionRequest debit) =>
        new(
            request.UserID,
            ticket.TradeDateTime!,
            ticket.SettlementDateTime!,
            request.Reason,
            [credit],
            [debit]);

    private static ResolvedBookCost? ResolveBookCost(TicketTradeAllocation allocation, Account account, Alpha3 settlementCurrency, FXRates? fxRates, List<string> messages)
    {
        if (allocation.BookCostOverride is decimal overrideValue)
            return new ResolvedBookCost(new TransactionBookCost(overrideValue), BookCostSource.ManualOverride, false);

        if (settlementCurrency == account.BookCurrency)
            return new ResolvedBookCost(new TransactionBookCost(allocation.SettlementAmount), BookCostSource.SameCurrency, false);

        if (fxRates is null)
        {
            messages.Add($"FX rates are required to estimate book cost from {settlementCurrency} to {account.BookCurrency}.");
            return null;
        }

        var rate = fxRates.Items.FirstOrDefault(item =>
            item.Active &&
            item.BaseCurrency == settlementCurrency &&
            item.QuoteCurrency == account.BookCurrency);

        if (rate is null)
        {
            messages.Add($"Missing FX {settlementCurrency}/{account.BookCurrency} for AccountID '{allocation.AccountID}'. Enter a book cost override or add the FX rate.");
            return null;
        }

        return new ResolvedBookCost(
            new TransactionBookCost(decimal.Round(allocation.SettlementAmount * rate.Price.Mid, 8)),
            BookCostSource.FxEstimate,
            true);
    }

    private static string FormatValidationNumber(decimal value) =>
        value.ToString("0.########");

    private sealed record TicketTradeApprovalTransactionLegs(TransactionRequest AssetLeg, TransactionRequest CashLeg);

    private sealed record ResolvedBookCost(TransactionBookCost BookCost, BookCostSource Source, bool Estimated);
}
