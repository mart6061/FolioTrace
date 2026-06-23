using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class TransactionBuilder
{
    public static Result<IReadOnlyList<ITransactionMovementEvent>> Create(TransactionSetRequest request, Holdings? holdings = null, Accounts? accounts = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var validationErrors = ValidateRequest(request, holdings, accounts);
        if (validationErrors.Count > 0)
            return Result<IReadOnlyList<ITransactionMovementEvent>>.Invalid(validationErrors);

        var auditDateTime = AuditDateTimeBuilder.Create();
        var eventSetID = EventSetIDBuilder.Create();
        var movementCount = request.Credits.Count + request.Debits.Count;
        var eventIDGroup = Enumerable.Range(0, movementCount)
            .Select(_ => new EventID(Guid.CreateGuid7()))
            .ToList();

        var events = new List<ITransactionMovementEvent>(movementCount);
        var eventIndex = 0;

        foreach (var credit in request.Credits)
        {
            events.Add(new TransactionCreditEvent(
                eventIDGroup[eventIndex++],
                request.UserID,
                request.EventDateTime,
                request.SettlementDateTime,
                auditDateTime,
                request.Reason,
                eventSetID,
                eventIDGroup,
                credit.HoldingID,
                credit.InstrumentID,
                credit.AccountID,
                credit.Quantity,
                credit.LocalCost,
                credit.LocalCostCurrency,
                credit.BookCost,
                credit.BookCostSource,
                credit.BookCostEstimated));
        }

        foreach (var debit in request.Debits)
        {
            events.Add(new TransactionDebitEvent(
                eventIDGroup[eventIndex++],
                request.UserID,
                request.EventDateTime,
                request.SettlementDateTime,
                auditDateTime,
                request.Reason,
                eventSetID,
                eventIDGroup,
                debit.HoldingID,
                debit.InstrumentID,
                debit.AccountID,
                debit.Quantity,
                debit.LocalCost,
                debit.LocalCostCurrency,
                debit.BookCost,
                debit.BookCostSource,
                debit.BookCostEstimated));
        }

        return Result<IReadOnlyList<ITransactionMovementEvent>>.Success(events);
    }

    private static List<string> ValidateRequest(TransactionSetRequest request, Holdings? holdings, Accounts? accounts)
    {
        var messages = new List<string>();
        if (request.UserID is null) messages.Add("UserID is required.");
        if (request.EventDateTime is null) messages.Add("EventDateTime is required.");
        if (request.SettlementDateTime is null) messages.Add("SettlementDateTime is required.");
        if (request.EventDateTime is not null && request.SettlementDateTime is not null && request.SettlementDateTime.Value.Date < request.EventDateTime.Value.Date)
            messages.Add("SettlementDateTime must be equal to or greater than EventDateTime.");
        if (string.IsNullOrWhiteSpace(request.Reason)) messages.Add("Reason is required.");
        if (request.Credits is null) messages.Add("Credits are required.");
        if (request.Debits is null) messages.Add("Debits are required.");
        if (holdings is null) messages.Add("Holdings are required.");

        if (request.Credits is null || request.Debits is null)
            return messages;

        if (request.Credits.Count == 0) messages.Add("At least one credit is required.");
        if (request.Debits.Count == 0) messages.Add("At least one debit is required.");
        if (request.Credits.Count + request.Debits.Count < 2) messages.Add("At least two transaction movements are required.");

        var accountIds = request.Credits
            .Concat(request.Debits)
            .Where(leg => leg?.AccountID is not null)
            .Select(leg => leg.AccountID)
            .Distinct()
            .ToList();
        if (accountIds.Count > 1)
            messages.Add("All transaction movements in a set must have the same AccountID.");
        ValidateAccountState(messages, request.EventDateTime, accountIds, accounts);

        ValidateLegs(messages, "Credit", request.Credits, holdings);
        ValidateLegs(messages, "Debit", request.Debits, holdings);

        var creditBookCost = request.Credits.Where(leg => leg?.BookCost is not null).Sum(leg => leg.BookCost.Value);
        var debitBookCost = request.Debits.Where(leg => leg?.BookCost is not null).Sum(leg => leg.BookCost.Value);
        if (creditBookCost != debitBookCost)
            messages.Add("Transaction book cost must balance: credits less debits must equal zero.");

        var creditLocalCost = request.Credits.Where(leg => leg?.LocalCost is not null).Sum(leg => leg.LocalCost.Value);
        var debitLocalCost = request.Debits.Where(leg => leg?.LocalCost is not null).Sum(leg => leg.LocalCost.Value);
        if (creditLocalCost != debitLocalCost)
            messages.Add("Transaction local cost must balance: credits less debits must equal zero.");

        return messages;
    }

    private static void ValidateAccountState(List<string> messages, EventDateTime? eventDateTime, IReadOnlyList<AccountID> accountIds, Accounts? accounts)
    {
        if (accounts is null || accountIds.Count != 1)
            return;

        var accountID = accountIds[0];
        var account = accounts.Items.SingleOrDefault(item => item.AccountID == accountID);
        if (account is null)
        {
            messages.Add($"No matching Account found for AccountID '{accountID}'.");
            return;
        }

        if (eventDateTime is not null && eventDateTime.Value < account.ValuationDateTime.Value)
            messages.Add($"Transaction EventDateTime must be on or after account StartDate '{account.ValuationDateTime.Value:O}'.");
        if (!account.Active)
            messages.Add($"AccountID '{accountID}' is inactive.");
    }

    private static void ValidateLegs(List<string> messages, string side, IReadOnlyList<TransactionRequest> legs, Holdings? holdings)
    {
        for (var index = 0; index < legs.Count; index++)
        {
            var leg = legs[index];
            if (leg is null)
            {
                messages.Add($"{side} {index + 1} is required.");
                continue;
            }

            if (leg.HoldingID is null) messages.Add($"{side} {index + 1} HoldingID is required.");
            if (leg.InstrumentID is null) messages.Add($"{side} {index + 1} InstrumentID is required.");
            if (leg.AccountID is null) messages.Add($"{side} {index + 1} AccountID is required.");
            if (leg.Quantity is null) messages.Add($"{side} {index + 1} Quantity is required.");
            if (leg.LocalCost is null) messages.Add($"{side} {index + 1} LocalCost is required.");
            if (leg.LocalCost is not null && leg.LocalCost.Value <= 0m) messages.Add($"{side} {index + 1} LocalCost must be greater than zero.");
            if (leg.LocalCostCurrency is null) messages.Add($"{side} {index + 1} LocalCostCurrency is required.");
            if (leg.BookCost is null) messages.Add($"{side} {index + 1} BookCost is required.");

            if (leg.HoldingID is null || holdings is null)
                continue;

            var holding = holdings.Items.SingleOrDefault(item => item.HoldingID == leg.HoldingID);
            if (holding is null)
            {
                messages.Add($"{side} {index + 1} HoldingID '{leg.HoldingID}' does not exist.");
                continue;
            }

            if (!holding.Active)
                messages.Add($"{side} {index + 1} HoldingID '{leg.HoldingID}' is inactive.");
            if (leg.AccountID is not null && holding.AccountID != leg.AccountID)
                messages.Add($"{side} {index + 1} HoldingID '{leg.HoldingID}' does not match AccountID '{leg.AccountID}'.");
            if (leg.InstrumentID is not null && holding.InstrumentID != leg.InstrumentID)
                messages.Add($"{side} {index + 1} HoldingID '{leg.HoldingID}' does not match InstrumentID '{leg.InstrumentID}'.");
        }
    }
}
