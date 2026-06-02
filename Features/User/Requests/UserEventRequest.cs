namespace FolioTrace.Aggregates;

public sealed record UserEventRequest(Guid UserID, DateTime EventDateTime, string Reason, string DisplayName, UserDisplayPreferencesRequest DisplayPreferences, UserProfileValuationPreferencesRequest ValuationPreferences);
