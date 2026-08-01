namespace FolioTrace.Aggregates;

public sealed record DateRangeRuleOption(Guid OptionID, DateControlOptionKind Kind, string Label, string? Expression, int DisplayOrder, bool IsDefault);
