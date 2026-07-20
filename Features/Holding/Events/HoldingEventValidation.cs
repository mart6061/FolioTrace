using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class HoldingEventValidation
{
    public static List<string> ValidateBase(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, HoldingID? holdingID)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);
        if (holdingID is null) messages.Add("HoldingID is required.");
        return messages;
    }

    public static void ValidateDefinition<TExpectedHolding>(List<string> messages, string? name, bool isDefault)
        where TExpectedHolding : HoldingBase
    {
        if (!HoldingKindRuntime.IsPositionMemo<TExpectedHolding>() && string.IsNullOrWhiteSpace(name))
            messages.Add("Name is required for all holding kinds except PositionMemo.");
    }

    public static void ValidateBankDetails(List<string> messages, string? bankName, string? accountName, SortCode? sortCode, BankAccountNumber? accountNumber, BIC? bic, IBAN? iban)
    {
        if (string.IsNullOrWhiteSpace(bankName)) messages.Add("BankName is required.");
        if (string.IsNullOrWhiteSpace(accountName)) messages.Add("AccountName is required.");
        if (sortCode is null) messages.Add("SortCode is required.");
        if (accountNumber is null) messages.Add("AccountNumber is required.");
        if (bic is null) messages.Add("BIC is required.");
        if (iban is null) messages.Add("IBAN is required.");
    }

    public static void ValidateReferences(List<string> messages, AccountID? accountID, InstrumentID? instrumentID, Accounts? accounts, Instruments? instruments)
    {
        if (accountID is null)
            messages.Add("AccountID is required.");
        else if (accounts is not null && !accounts.Items.Any(account => account.AccountID == accountID))
            messages.Add($"No matching Account found for AccountID '{accountID}'.");

        if (instrumentID is null)
            messages.Add("InstrumentID is required.");
        else if (instruments is not null && !instruments.Items.Any(instrument => instrument.InstrumentID == instrumentID))
            messages.Add($"No matching Instrument found for InstrumentID '{instrumentID}'.");
    }

    public static void ValidateCreatedHolding<TExpectedHolding>(List<string> messages, HoldingID? holdingID, AccountID? accountID, InstrumentID? instrumentID, bool isDefault, Holdings? holdings)
        where TExpectedHolding : HoldingBase
    {
        if (holdingID is null || holdings is null)
            return;

        if (holdings.Items.Any(holding => holding.HoldingID == holdingID))
            messages.Add($"HoldingID '{holdingID}' already exists.");

        ValidateDefaultForKind<TExpectedHolding>(messages, holdingID, accountID, instrumentID, isDefault, holdings);
    }

    public static HoldingBase? ValidateModifiedHolding<TExpectedHolding>(List<string> messages, HoldingID? holdingID, bool isDefault, Holdings? holdings)
        where TExpectedHolding : HoldingBase
    {
        if (holdingID is null || holdings is null)
            return null;

        var holding = holdings.Items.SingleOrDefault(item => item.HoldingID == holdingID);
        if (holding is null)
        {
            messages.Add($"No matching Holding found for HoldingID '{holdingID}'.");
            return null;
        }

        if (holding is not TExpectedHolding)
            messages.Add($"HoldingID '{holdingID}' is a {holding.GetHoldingKindName()} holding, not a {HoldingKindRuntime.GetKindName<TExpectedHolding>()} holding.");

        ValidateDefaultForKind<TExpectedHolding>(messages, holdingID, holding.AccountID, holding.InstrumentID, isDefault, holdings);
        return holding;
    }

    public static void ValidateActiveHolding(List<string> messages, HoldingID? holdingID, Holdings? holdings)
    {
        if (holdingID is null || holdings is null)
            return;

        if (!holdings.Items.Any(holding => holding.HoldingID == holdingID))
            messages.Add($"No matching Holding found for HoldingID '{holdingID}'.");
    }

    private static void ValidateDefaultForKind<TExpectedHolding>(List<string> messages, HoldingID holdingID, AccountID? accountID, InstrumentID? instrumentID, bool isDefault, Holdings holdings)
        where TExpectedHolding : HoldingBase
    {
        if (!isDefault || accountID is null || instrumentID is null)
            return;

        var holdingKind = HoldingKindRuntime.GetKindName<TExpectedHolding>();
        var isNominal = HoldingKindRuntime.IsNominal<TExpectedHolding>();
        if (holdings.Items.Any(holding =>
            holding.HoldingID != holdingID &&
            holding.AccountID == accountID &&
            holding.GetHoldingKindName() == holdingKind &&
            (isNominal || holding.InstrumentID == instrumentID) &&
            holding.Default))
        {
            messages.Add(isNominal
                ? $"A default {holdingKind} holding already exists for AccountID '{accountID}'."
                : $"A default {holdingKind} holding already exists for AccountID '{accountID}' and InstrumentID '{instrumentID}'.");
        }
    }
}
