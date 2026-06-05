using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserValuationPreferencesRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    UserValuationDateOption ValuationDateOption,
    HoldingDateBasis HoldingDateBasis,
    bool ShowZeroBalances);
