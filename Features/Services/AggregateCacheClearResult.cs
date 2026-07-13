namespace Services;

public sealed record AggregateCacheClearResult(
    int Accounts,
    int Brokers,
    int Countries,
    int Currencies,
    int FXs,
    int FXRates,
    int Holdings,
    int HoldingPositions,
    int Instruments,
    int InstrumentValues,
    int Tickets,
    int Users,
    int UserMenuPreferences,
    int UserValuationPreferences,
    int UserBookmarks,
    int InputControlSettings,
    int ValuationSettings,
    int AssetAllocationMappings,
    int ReportConfigs);
