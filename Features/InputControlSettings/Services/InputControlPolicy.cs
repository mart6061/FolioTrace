namespace FolioTrace.Aggregates;

public sealed record InputControlPolicy(
    InputControlKind ControlKind,
    short DecimalPlaces,
    decimal? MinValue,
    decimal? MaxValue,
    string FormatPattern,
    string FormatSource,
    bool AllowNegative,
    string? Currency,
    IReadOnlyList<string> ValidationMessages);
