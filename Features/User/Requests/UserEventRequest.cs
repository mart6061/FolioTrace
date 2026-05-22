namespace FolioTrace.Aggregates;

public sealed record UserEventRequest(Guid UserID, DateTime EventDateTime, string Reason, string DisplayName, UserDisplayPreferencesRequest DisplayPreferences, UserValuationPreferencesRequest ValuationPreferences);

public sealed record UserDisplayPreferencesRequest(bool DarkMode, bool RememberTraceDate);

public sealed record UserValuationPreferencesRequest(DateTime ValuationDate, bool ShowIncome, bool ShowBook);
