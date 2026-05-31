using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class AccountEventValidation
{
    public static List<string> ValidateAccountChange(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, AccountID? accountID, string? name, string? formalName)
    {
        var messages = new List<string>();
        if (eventId is null) messages.Add("EventID is required.");
        if (userId is null) messages.Add("UserID is required.");
        if (eventDateTime is null) messages.Add("EventDateTime is required.");
        if (auditDateTime is null) messages.Add("AuditDateTime is required.");
        if (string.IsNullOrWhiteSpace(reason)) messages.Add("Reason is required.");
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
        var messages = new List<string>();
        if (eventId is null) messages.Add("EventID is required.");
        if (userId is null) messages.Add("UserID is required.");
        if (eventDateTime is null) messages.Add("EventDateTime is required.");
        if (auditDateTime is null) messages.Add("AuditDateTime is required.");
        if (string.IsNullOrWhiteSpace(reason)) messages.Add("Reason is required.");
        if (accountID is null) messages.Add("AccountID is required.");
        if (displayOrder is null || displayOrder.Value < 1) messages.Add("DisplayOrder must be positive.");
        return messages;
    }
}
