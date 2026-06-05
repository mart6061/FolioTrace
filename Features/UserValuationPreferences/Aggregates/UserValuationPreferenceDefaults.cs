namespace FolioTrace.Aggregates;

public static class UserValuationPreferenceDefaults
{
    public const UserValuationDateOption ValuationDateOption = UserValuationDateOption.TodayEndOfDay;

    public const HoldingDateBasis HoldingDateBasis = FolioTrace.Aggregates.HoldingDateBasis.EventDateTime;

    public const bool ShowZeroBalances = false;
}
