namespace FolioTrace.Aggregates;

public static class UserValuationPreferenceDefaults
{
    public const UserValuationDateOption ValuationDateOption = UserValuationDateOption.TodayEndOfDay;

    public const ValuationDateBasis ValuationDateBasis = FolioTrace.Aggregates.ValuationDateBasis.EventDateTime;

    public const bool ShowZeroBalances = false;
}
