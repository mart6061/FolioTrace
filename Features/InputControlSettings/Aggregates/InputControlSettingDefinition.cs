using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InputControlSettingDefinition(
    InputControlKind ControlKind,
    InputControlSettingScope Scope,
    AccountID? AccountID,
    UserID? UserID,
    short? DecimalPlaces,
    decimal? MinValue,
    decimal? MaxValue,
    string? FormatPattern,
    bool? AllowNegative);
