using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class HoldingEventValidation
{
    public static List<string> ValidateBase(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, HoldingID? holdingID)
    {
        var messages = new List<string>();
        if (eventId is null) messages.Add("EventID is required.");
        if (userId is null) messages.Add("UserID is required.");
        if (eventDateTime is null) messages.Add("EventDateTime is required.");
        if (auditDateTime is null) messages.Add("AuditDateTime is required.");
        if (string.IsNullOrWhiteSpace(reason)) messages.Add("Reason is required.");
        if (holdingID is null) messages.Add("HoldingID is required.");
        return messages;
    }

    public static void ValidateDefinition(List<string> messages, HoldingType holdingType, HoldingNominalType? nominalType, string? name, bool isDefault)
    {
        if (holdingType is HoldingType.Nominal)
        {
            if (nominalType is null)
                messages.Add("NominalType is required for Nominal holdings.");
        }
        else if (nominalType is not null)
        {
            messages.Add("NominalType can only be set for Nominal holdings.");
        }

        if (holdingType is not HoldingType.CashOnHand && isDefault)
            messages.Add("Default can only be set for CashOnHand holdings.");

        if (holdingType is not HoldingType.Position && string.IsNullOrWhiteSpace(name))
            messages.Add("Name is required for Nominal, CashOnHand, and CashDebt holdings.");
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

    public static void ValidateCreatedHolding(List<string> messages, HoldingID? holdingID, AccountID? accountID, InstrumentID? instrumentID, HoldingType holdingType, bool isDefault, Holdings? holdings)
    {
        if (holdingID is null || holdings is null)
            return;

        if (holdings.Items.Any(holding => holding.HoldingID == holdingID))
            messages.Add($"HoldingID '{holdingID}' already exists.");

        ValidateDefaultCashOnHand(messages, holdingID, accountID, instrumentID, holdingType, isDefault, holdings);
    }

    public static void ValidateModifiedHolding(List<string> messages, HoldingID? holdingID, bool isDefault, Holdings? holdings)
    {
        if (holdingID is null || holdings is null)
            return;

        var holding = holdings.Items.SingleOrDefault(item => item.HoldingID == holdingID);
        if (holding is null)
        {
            messages.Add($"No matching Holding found for HoldingID '{holdingID}'.");
            return;
        }

        ValidateDefaultCashOnHand(messages, holdingID, holding.AccountID, holding.InstrumentID, holding.HoldingType, isDefault, holdings);
    }

    public static void ValidateActiveHolding(List<string> messages, HoldingID? holdingID, Holdings? holdings)
    {
        if (holdingID is null || holdings is null)
            return;

        if (!holdings.Items.Any(holding => holding.HoldingID == holdingID))
            messages.Add($"No matching Holding found for HoldingID '{holdingID}'.");
    }

    private static void ValidateDefaultCashOnHand(List<string> messages, HoldingID holdingID, AccountID? accountID, InstrumentID? instrumentID, HoldingType holdingType, bool isDefault, Holdings holdings)
    {
        if (!isDefault || holdingType is not HoldingType.CashOnHand || accountID is null || instrumentID is null)
            return;

        if (holdings.Items.Any(holding =>
            holding.HoldingID != holdingID &&
            holding.AccountID == accountID &&
            holding.InstrumentID == instrumentID &&
            holding.HoldingType is HoldingType.CashOnHand &&
            holding.Default))
        {
            messages.Add($"A default CashOnHand holding already exists for AccountID '{accountID}' and InstrumentID '{instrumentID}'.");
        }
    }
}
