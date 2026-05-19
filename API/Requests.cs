namespace API;

public sealed record CurrencyEventRequest(Guid UserID, DateTime EventDateTime, string Reason, string AlphabeticCode, int NumericCode, short DecimalPlace, string Name);

public sealed record UserEventRequest(Guid UserID, DateTime EventDateTime, string Reason, string DisplayName, UserDisplayPreferencesRequest DisplayPreferences, UserValuationPreferencesRequest ValuationPreferences);

public sealed record UserDisplayPreferencesRequest(bool DarkMode, bool RememberTraceDate);

public sealed record UserValuationPreferencesRequest(DateTime ValuationDate, bool ShowIncome, bool ShowBook);
