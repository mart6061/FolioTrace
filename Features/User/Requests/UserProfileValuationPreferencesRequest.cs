namespace FolioTrace.Aggregates;

public sealed record UserProfileValuationPreferencesRequest(DateTime ValuationDate, bool ShowIncome, bool ShowBook);
