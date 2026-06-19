using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationNodeAccountSetting(
    AccountID AccountID,
    decimal? TargetWeight,
    decimal? TargetWeightMax,
    decimal? TargetWeightMin,
    decimal? TargetYield);
