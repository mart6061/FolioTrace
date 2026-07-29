using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserValuationPreferencesRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    UserValuationDateOption ValuationDateOption,
    HoldingDateBasis HoldingDateBasis,
    ValuationPriceConvention ValuationPriceConvention,
    bool ShowZeroBalances)
{
    public UserValuationDateOption? StartValuationDateOption { get; init; }

    public UserValuationDateOption? EndValuationDateOption { get; init; }
}
