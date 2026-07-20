using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class AccountEventValidation
{
    public static List<string> ValidateCommon(EventID? eventID, UserID? userID, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, AccountID? accountID)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventID, userID, eventDateTime, auditDateTime, reason);
        if (accountID is null) messages.Add("AccountID is required.");
        return messages;
    }

    public static List<string> ValidateAccountChange(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, AccountID? accountID, string? name, string? formalName)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);
        if (accountID is null) messages.Add("AccountID is required.");
        if (string.IsNullOrWhiteSpace(name)) messages.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(formalName)) messages.Add("FormalName is required.");
        return messages;
    }

    public static void ValidateBookCurrency(List<string> messages, Alpha3? bookCurrency, Currencies? currencies)
    {
        if (bookCurrency is null || currencies is null)
            return;

        if (!currencies.Items.Any(currency => currency.AlphabeticCode == bookCurrency))
            messages.Add($"BookCurrency '{bookCurrency}' does not exist in Currencies.");
    }

    public static void ValidateBookCostBasis(List<string> messages, ProfitLossMethod bookCostBasis)
    {
        if (!Enum.IsDefined(bookCostBasis))
            messages.Add($"BookCostBasis '{bookCostBasis}' is not supported.");
    }

    public static void ValidateCreatedName(List<string> messages, string? name, Accounts? accounts)
    {
        if (string.IsNullOrWhiteSpace(name) || accounts is null)
            return;

        if (accounts.Items.Any(account => string.Equals(account.Name, name, StringComparison.OrdinalIgnoreCase)))
            messages.Add($"Account Name '{name}' already exists.");
    }

    public static void ValidateModifiedAccount(List<string> messages, AccountID? accountID, string? name, Accounts? accounts)
    {
        if (accountID is null)
            return;

        if (accounts is null)
            return;

        if (!accounts.Items.Any(account => account.AccountID == accountID))
        {
            messages.Add($"No matching Account found for AccountID '{accountID}'.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(name) && accounts.Items.Any(account => account.AccountID != accountID && string.Equals(account.Name, name, StringComparison.OrdinalIgnoreCase)))
            messages.Add($"Account Name '{name}' already exists.");
    }

    public static List<string> ValidateAccountDisplayOrder(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, AccountID? accountID, DisplayOrder? displayOrder)
    {
        var messages = EventFieldValidation.CommonFieldMessages(eventId, userId, eventDateTime, auditDateTime, reason);
        if (accountID is null) messages.Add("AccountID is required.");
        if (displayOrder is null || displayOrder.Value < 1) messages.Add("DisplayOrder must be positive.");
        return messages;
    }
}
