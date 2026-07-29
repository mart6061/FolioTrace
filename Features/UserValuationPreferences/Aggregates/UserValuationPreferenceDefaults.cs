namespace FolioTrace.Aggregates;

public static class UserValuationPreferenceDefaults
{
    public const UserValuationDateOption ValuationDateOption = UserValuationDateOption.TodayEndOfDay;

    public const UserValuationDateOption StartValuationDateOption = UserValuationDateOption.TodayEndOfDay;

    public const UserValuationDateOption EndValuationDateOption = UserValuationDateOption.TodayEndOfDay;

    public const HoldingDateBasis HoldingDateBasis = FolioTrace.Aggregates.HoldingDateBasis.EventDateTime;

    /// <summary>
    /// Clean by default. Bonds are conventionally quoted clean, it is the more informative view because the
    /// accrued shows on its own sub-line, and it is what preference events written before this setting
    /// existed replay as. The final total includes accrued interest either way.
    /// </summary>
    public const ValuationPriceConvention ValuationPriceConvention = FolioTrace.Aggregates.ValuationPriceConvention.Clean;

    public const bool ShowZeroBalances = false;
}
